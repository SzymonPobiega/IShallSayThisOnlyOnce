using System;
using System.Threading.Tasks;
using Messages;
using NServiceBus.Pipeline;

class BlowUpWhenPublishingBehavior : Behavior<IOutgoingLogicalMessageContext>
{
    static DateTime lastAttempt = DateTime.MinValue;

    public override async Task Invoke(IOutgoingLogicalMessageContext context, Func<Task> next)
    {
        if (context.Message.Instance is ItemAdded added && added.Filling == Filling.Ruskie)
        {
            if (lastAttempt.AddSeconds(10) < DateTime.UtcNow)
            {
                lastAttempt = DateTime.UtcNow;
                throw new Exception("Broker error");
            }
        }
        await next();
    }
}