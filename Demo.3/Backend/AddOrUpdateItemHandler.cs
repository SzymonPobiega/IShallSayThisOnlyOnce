using System;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Messages;
using NServiceBus;
using NServiceBus.Logging;
using NServiceBus.Settings;

class AddOrUpdateItemHandler : IHandleMessages<AddOrUpdateItem>
{
    public async Task Handle(AddOrUpdateItem message, IMessageHandlerContext context)
    {
        var messageId = Guid.Parse(context.MessageId);
        var nextMessageId = GuidUtility.CreateDeterministicGuid(messageId, ReadOnlySettings.EndpointName());

        var dbContext = context.Extensions.Get<BackendDataContext>();
        if (!dbContext.Processed)
        {
            var order = await dbContext.Orders.FirstAsync(o => o.OrderId == message.OrderId).ConfigureAwait(false);
            var line = new OrderLine
            {
                Filling = message.Filling,
                Quantity = message.Quantity
            };
            await Task.Delay(3000).ConfigureAwait(false);
            order.Lines.Add(line);
            log.Info($"Item {message.Filling} added.");
        }
        else
        {
            log.Info("Duplicate or partially failed AddItem message detected. Repubishing outgoing messages.");
        }
        var options = new PublishOptions();
        options.SetMessageId(nextMessageId.ToString());
        await context.Publish(new ItemAddedOrUpdated
        {
            Filling = message.Filling,
            OrderId = message.OrderId,
            Quantity = message.Quantity
        },options).ConfigureAwait(false);
    }

    public ReadOnlySettings ReadOnlySettings { get; set; }
    static readonly ILog log = LogManager.GetLogger<AddOrUpdateItemHandler>();
}