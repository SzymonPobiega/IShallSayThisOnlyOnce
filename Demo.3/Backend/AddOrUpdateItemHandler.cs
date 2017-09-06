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
        }

        await context.Publish(new ItemAddedOrUpdated
        {
            Filling = message.Filling,
            OrderId = message.OrderId,
            Quantity = message.Quantity
        }).ConfigureAwait(false);
    }

    static readonly ILog log = LogManager.GetLogger<AddOrUpdateItemHandler>();
}