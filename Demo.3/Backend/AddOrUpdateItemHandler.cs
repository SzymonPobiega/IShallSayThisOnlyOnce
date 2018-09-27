using System;
using System.Data.Entity;
using System.Threading.Tasks;
using Messages;
using NServiceBus;
using NServiceBus.Logging;

class AddOrUpdateItemHandler : IHandleMessages<AddOrUpdateItem>
{
    public async Task Handle(AddOrUpdateItem message, IMessageHandlerContext context)
    {
        var dbContext = context.Extensions.Get<OrdersDataContext>();
            
        if (!dbContext.Processed)
        {
            var order = await dbContext.Orders
                .FirstAsync(o => o.OrderId == message.OrderId);

            var line = new OrderLine(message.Filling, message.Quantity);
            order.Lines.Add(line);
            log.Info($"Item {message.Filling} added.");
        }
        else
        {
            log.Info("Duplicate or partially failed AddItem message detected. Repubishing outgoing messages.");
        }
        var options = new PublishOptions();
        var messageId = Guid.Parse(context.MessageId);
        var nextMessageId = Ultis.DeterministicGuid(messageId, "Orders");
        options.SetMessageId(nextMessageId.ToString());

        await context.Publish(new ItemAddedOrUpdated
        {
            Filling = message.Filling,
            OrderId = message.OrderId,
            Quantity = message.Quantity
        },options);
    }

    static readonly ILog log = LogManager.GetLogger<AddOrUpdateItemHandler>();
}