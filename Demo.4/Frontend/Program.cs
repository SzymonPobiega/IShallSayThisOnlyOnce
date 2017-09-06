using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Messages;
using NServiceBus;
using NServiceBus.Logging;
using NServiceBus.Serilog;
using Serilog;
using Serilog.Filters;

namespace Frontend
{
    class Program
    {
        static void Main(string[] args)
        {
            Start().GetAwaiter().GetResult();
        }

        static readonly Regex submitExpr = new Regex("submit ([A-Za-z]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        static readonly Regex addExpr = new Regex($"add ({string.Join("|", Enum.GetNames(typeof(Filling)))}) to ([A-Za-z]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        static async Task Start()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Filter.ByExcluding(Matching.FromSource("NServiceBus.PerformanceMonitorUsersInstaller"))
                .Filter.ByExcluding(Matching.FromSource("NServiceBus.QueuePermissions"))
                .WriteTo.Console()
                .CreateLogger();

            LogManager.Use<SerilogFactory>();

            Console.Title = "OnlyOnce.Demo4.Frontend";

            var config = new EndpointConfiguration("OnlyOnce.Demo4.Frontend");
            config.UsePersistence<InMemoryPersistence>();
            config.SendFailedMessagesTo("error");
            config.Pipeline.Register(new DuplicateMessagesBehavior(), "Duplicates outgoing messages");
            var routing = config.UseTransport<MsmqTransport>().Routing();
            routing.RouteToEndpoint(typeof(SubmitOrder).Assembly, "OnlyOnce.Demo4.Backend");
            config.EnableInstallers();

            var endpoint = await Endpoint.Start(config).ConfigureAwait(false);

            while (true)
            {
                Console.WriteLine("Type 'submit XYZ' to create a new order with ID XYZ.");
                Console.WriteLine("Type 'add <filling> to XYZ' to add pierogi with selected filling to order XYZ.");
                Console.WriteLine("Available fillings: " + string.Join(",", Enum.GetNames(typeof(Filling))));
                var command = Console.ReadLine();

                if (string.IsNullOrEmpty(command))
                {
                    break;
                }

                var match = submitExpr.Match(command);
                if (match.Success)
                {
                    var orderId = match.Groups[1].Value;
                    var message = new SubmitOrder
                    {
                        OrderId = orderId
                    };
                    await endpoint.Send(message).ConfigureAwait(false);
                    continue;
                }
                match = addExpr.Match(command);
                if (match.Success)
                {
                    var filling = match.Groups[1].Value;
                    var orderId = match.Groups[2].Value;
                    var message = new AddOrUpdateItem
                    {
                        OrderId = orderId,
                        Filling = (Filling)Enum.Parse(typeof(Filling), filling)
                    };
                    await endpoint.Send(message).ConfigureAwait(false);
                    continue;
                }
                Console.WriteLine("Unrecognized command.");
            }

            await endpoint.Stop().ConfigureAwait(false);
        }
    }
}