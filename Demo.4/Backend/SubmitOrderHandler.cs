using System;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Messages;
using NServiceBus;
using NServiceBus.Logging;

class SubmitOrderHandler : IHandleMessages<SubmitOrder>
{
    public async Task Handle(SubmitOrder message, IMessageHandlerContext context)
    {
        var persistenceSession = context.SynchronizedStorageSession.SqlPersistenceSession();
        var dbContext = new BackendDataContext(persistenceSession.Connection, persistenceSession.Transaction);
        
        var order = new Order
        {
            OrderId = message.OrderId
        };
        dbContext.Orders.Add(order);

        await dbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    static readonly ILog log = LogManager.GetLogger<SubmitOrderHandler>();
}