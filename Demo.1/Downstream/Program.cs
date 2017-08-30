using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Messages;
using NServiceBus;

namespace Downstream
{
    class Program
    {
        static void Main(string[] args)
        {
            Start().GetAwaiter().GetResult();
        }

        static async Task Start()
        {
            Console.Title = "OnlyOnce.Demo1.Downstream";

            var config = new EndpointConfiguration("OnlyOnce.Demo1.Downstream");
            config.UsePersistence<InMemoryPersistence>();
            var transport = config.UseTransport<MsmqTransport>();
            transport.Transactions(TransportTransactionMode.ReceiveOnly);
            transport.Routing().RegisterPublisher(typeof(ItemAdded), "OnlyOnce.Demo1.Backend");
            config.Recoverability().Immediate(x => x.NumberOfRetries(5));
            config.Recoverability().Delayed(x => x.NumberOfRetries(0));
            config.SendFailedMessagesTo("error");

            var endpoint = await Endpoint.Start(config).ConfigureAwait(false);

            while (true)
            {
                Console.WriteLine("Press <enter> to exit.");
                Console.ReadLine();
            }
        }
    }
}
