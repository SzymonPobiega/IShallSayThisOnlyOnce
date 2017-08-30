using System;
using System.Data.Entity.Validation;
using System.Data.SqlClient;
using System.Threading.Tasks;
using NServiceBus;

class Program
{
    public const string ConnectionString = @"Data Source=.\SqlExpress;Database=OnlyOnce.Demo1.Backend;Integrated Security=True";

    static void Main(string[] args)
    {
        Start().GetAwaiter().GetResult();
    }

    static async Task Start()
    {
        Console.Title = "OnlyOnce.Demo1.Backend";

        var config = new EndpointConfiguration("OnlyOnce.Demo1.Backend");
        config.UsePersistence<InMemoryPersistence>();
        config.UseTransport<MsmqTransport>().Transactions(TransportTransactionMode.ReceiveOnly);
        config.Recoverability().Immediate(x => x.NumberOfRetries(5));
        config.Recoverability().Delayed(x => x.NumberOfRetries(0));
        config.Recoverability().AddUnrecoverableException(typeof(DbEntityValidationException));
        config.SendFailedMessagesTo("error");

        SqlHelper.EnsureDatabaseExists(ConnectionString);

        using (var receiverDataContext = new BackendDataContext(new SqlConnection(ConnectionString)))
        {
            receiverDataContext.Database.Initialize(true);
        }

        var endpoint = await Endpoint.Start(config).ConfigureAwait(false);

        while (true)
        {
            Console.WriteLine("Press <enter> to exit.");
            Console.ReadLine();
        }
    }
}