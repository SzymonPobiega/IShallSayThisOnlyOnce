using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Messages;
using NServiceBus;
using NServiceBus.Logging;

class AddOrUpdateItemHandler : IHandleMessages<AddOrUpdateItem>
{
    public async Task Handle(AddOrUpdateItem message, IMessageHandlerContext context)
    {
        var session = context
            .SynchronizedStorageSession.SqlPersistenceSession();

        var dbContext = new OrdersDataContext(session.Connection, 
            session.Transaction);

        var order = await dbContext.Orders
            .FirstAsync(o => o.OrderId == message.OrderId);

        var existingLine = order.Lines
            .FirstOrDefault(x => x.Filling == message.Filling);

        if (existingLine != null)
        {
            existingLine.Quantity = message.Quantity;
        }
        else
        {
            var line = new OrderLine(message.Filling, message.Quantity);
            order.Lines.Add(line);
        }

        await context.Publish(new ItemAddedOrUpdated
        {
            Filling = message.Filling,
            OrderId = message.OrderId,
            Quantity = message.Quantity
        });
        await dbContext.SaveChangesAsync();
        log.Info($"Item {message.Filling} added.");
    }

    static readonly ILog log = LogManager.GetLogger<AddOrUpdateItemHandler>();
}