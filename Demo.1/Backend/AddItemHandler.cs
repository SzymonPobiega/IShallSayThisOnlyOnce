using System.Data.Entity;
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
        var dbContext = new OrdersDataContext(new SqlConnection(Program.ConnectionString));

        var order = await dbContext.Orders.FirstAsync(o => o.OrderId == message.OrderId);

        if (order.Lines.Any(x => x.Filling == message.Filling))
        {
            log.Info("Duplicate AddItem message detected. Ignoring.");
            return;
        }

        var line = new OrderLine
        {
            Filling = message.Filling
        };
        order.Lines.Add(line);

        await context.PublishImmediately(new ItemAdded
        {
            Filling = message.Filling,
            OrderId = message.OrderId
        });

        await dbContext.SaveChangesAsync();
        log.Info($"Item {message.Filling} added.");
    }

    static readonly ILog log = LogManager.GetLogger<AddItemHandler>();
}