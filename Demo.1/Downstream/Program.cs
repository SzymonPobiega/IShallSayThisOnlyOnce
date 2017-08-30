using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Messages;
using NServiceBus;
using NServiceBus.Logging;
using NServiceBus.Serilog;
using Serilog;
using Serilog.Events;
using Serilog.Filters;

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
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Filter.ByExcluding(Matching.FromSource("NServiceBus.PerformanceMonitorUsersInstaller"))
                .Filter.ByExcluding(Matching.FromSource("NServiceBus.QueuePermissions"))
                .WriteTo.Console()
                .CreateLogger();

            LogManager.Use<SerilogFactory>();

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
