using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Messages;
using NServiceBus;

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
            var config = new EndpointConfiguration("OnlyOnce.Demo1.Frontend");
            config.UsePersistence<InMemoryPersistence>();
            config.SendFailedMessagesTo("error");
            config.Pipeline.Register(new DuplicateMessagesBehavior(), "Duplicates outgoing messages");
            var routing = config.UseTransport<MsmqTransport>().Routing();
            routing.RouteToEndpoint(typeof(SubmitOrder).Assembly, "OnlyOnce.Demo1.Backend");

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
                    var message = new AddItem
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