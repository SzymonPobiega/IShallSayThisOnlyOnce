using System;
using System.Threading.Tasks;
using NServiceBus.Pipeline;

class DuplicateMessagesBehavior : Behavior<IDispatchContext>
{
    Random r = new Random();

    public override async Task Invoke(IDispatchContext context, Func<Task> next)
    {
        await next();
        if (r.Next(1) == 0) //50% chance of sending duplicates
        {
            await next();
        }
    }
}