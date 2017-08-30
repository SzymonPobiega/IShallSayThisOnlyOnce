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
        var dbContext = new BackendDataContext(new SqlConnection(Program.ConnectionString));

        var order = await dbContext.Orders.FirstAsync(o => o.OrderId == message.OrderId).ConfigureAwait(false);

        if (order.Lines.Any(x => x.Filling == message.Filling))
        {
            log.Info("Duplicate AddItem message detected. Ignoring");
            return;
        }

        var line = new OrderLine
        {
            Filling = message.Filling,
            Quantity = message.Quantity
        };
        order.Lines.Add(line);

        var options = new PublishOptions();
        options.RequireImmediateDispatch();
        await context.Publish(new ItemAdded
        {
            Filling = message.Filling,
            OrderId = message.OrderId,
            Quantity = message.Quantity
        }, options).ConfigureAwait(false);

        await dbContext.SaveChangesAsync()
            .ConfigureAwait(false);
    }

    static readonly ILog log = LogManager.GetLogger<AddItemHandler>();
}