using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Messages;
using NServiceBus;
using NServiceBus.Logging;

class AddOrUpdateItemHandler : IHandleMessages<AddOrUpdateItem>
{
    public async Task Handle(AddOrUpdateItem message, IMessageHandlerContext context)
    {
        var persistenceSession = context.SynchronizedStorageSession.SqlPersistenceSession();
        var dbContext = new BackendDataContext(persistenceSession.Connection, persistenceSession.Transaction);

        var order = await dbContext.Orders.FirstAsync(o => o.OrderId == message.OrderId).ConfigureAwait(false);
        var existingLine = order.Lines.FirstOrDefault(x => x.Filling == message.Filling);
        if (existingLine != null)
        {
            existingLine.Quantity = message.Quantity;
        }
        else
        {
            var line = new OrderLine
            {
                Filling = message.Filling,
                Quantity = message.Quantity
            };
            order.Lines.Add(line);
        }

        //Simulate some lengthy operation
        await Task.Delay(3000).ConfigureAwait(false);

        await context.Publish(new ItemAddedOrUpdated
        {
            Filling = message.Filling,
            OrderId = message.OrderId,
            Quantity = message.Quantity
        }).ConfigureAwait(false);
        await dbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    static readonly ILog log = LogManager.GetLogger<AddOrUpdateItemHandler>();
}