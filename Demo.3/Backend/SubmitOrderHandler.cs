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
        var dbContext = context.Extensions.Get<OrdersDataContext>();
        if (dbContext.Processed)
        {
            return;
        }

        var order = new Order
        {
            OrderId = message.OrderId
        };
        dbContext.Orders.Add(order);
        log.Info($"Order {message.OrderId} created.");
    }

    static readonly ILog log = LogManager.GetLogger<SubmitOrderHandler>();
}