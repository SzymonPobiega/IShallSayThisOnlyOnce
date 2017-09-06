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
        var processedMessage = await dbContext.ProcessedMessages.FirstOrDefaultAsync(m => m.MessageId == context.MessageId)
            .ConfigureAwait(false);

        if (processedMessage != null)
        {
            //We processed the message but we are not sure if we dispatched the outgoing messages.
            dbContext.Processed = true;
        }
        else
        {
            dbContext.ProcessedMessages.Add(new ProcessedMessage {MessageId = context.MessageId});
        }

        context.Extensions.Set(dbContext);

        await next().ConfigureAwait(false);

        await dbContext.SaveChangesAsync()
            .ConfigureAwait(false);
    }
}