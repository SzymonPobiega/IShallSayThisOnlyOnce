using System;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Threading.Tasks;
using NServiceBus.Pipeline;

class DeduplicatingBehavior : Behavior<IIncomingLogicalMessageContext>
{
    public override async Task Invoke(IIncomingLogicalMessageContext context, Func<Task> next)
    {
        var dbContext = new BackendDataContext(new SqlConnection(Program.ConnectionString));
        var processedMessage = await dbContext.ProcessedMessages
            .FirstOrDefaultAsync(m => m.MessageId == context.MessageId);

        if (processedMessage != null)
        {
            dbContext.Processed = true;
        }
        else
        {
            dbContext.ProcessedMessages.Add(new ProcessedMessage {MessageId = context.MessageId});
        }

        context.Extensions.Set(dbContext);

        await next().ConfigureAwait(false); //Process

        await dbContext.SaveChangesAsync()
            .ConfigureAwait(false);
    }
}