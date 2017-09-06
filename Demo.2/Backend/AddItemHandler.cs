﻿using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Messages;
using NServiceBus;
using NServiceBus.Logging;

class AddItemHandler : IHandleMessages<AddItem>
{
    public async Task Handle(AddItem message, IMessageHandlerContext context)
    {
        var dbContext = new BackendDataContext(new SqlConnection(Program.ConnectionString));

        var order = await dbContext.Orders.FirstAsync(o => o.OrderId == message.OrderId).ConfigureAwait(false);

        if (order.Lines.Any(x => x.Id == context.MessageId))
        {
            log.Info("Duplicate AddItem message detected. Ignoring");
            return;
        }

        var line = new OrderLine
        {
            Id = context.MessageId,
            Filling = message.Filling,
            Quantity = message.Quantity
        };
        await Task.Delay(3000).ConfigureAwait(false);

        order.Lines.Add(line);

        var publishOptions = new PublishOptions();
        publishOptions.RequireImmediateDispatch();
        await context.Publish(new ItemAdded
        {
            Filling = message.Filling,
            OrderId = message.OrderId,
            Quantity = message.Quantity
        }, publishOptions).ConfigureAwait(false);

        await dbContext.SaveChangesAsync()
            .ConfigureAwait(false);
    }

    static readonly ILog log = LogManager.GetLogger<AddItemHandler>();
}