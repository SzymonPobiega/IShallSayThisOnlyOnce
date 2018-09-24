using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Messages;
using NServiceBus;
using NServiceBus.Logging;

class AddItemHandler : IHandleMessages<AddItem>
{
    public async Task Handle(AddItem message, 
        IMessageHandlerContext context)
    {
        var dbContext = new OrdersDataContext();

        var order = await dbContext.Orders
            .FirstAsync(o => o.OrderId == message.OrderId);

        if (order.Lines.Any(x => x.Id == context.MessageId))
        {
            log.Info("Duplicate or partially failed AddItem message detected. Repubishing outgoing messages.");
        }
        else
        {
            var line = new OrderLine
            {
                Id = context.MessageId,
                Filling = message.Filling,
                Quantity = message.Quantity
            };
            order.Lines.Add(line);
            await dbContext.SaveChangesAsync();
            log.Info($"Item {message.Filling} added.");
        }

        await context.Publish(new ItemAdded
        {
            Filling = message.Filling,
            OrderId = message.OrderId,
            Quantity = message.Quantity
        });
        
    }

    static readonly ILog log = LogManager.GetLogger<AddItemHandler>();
}