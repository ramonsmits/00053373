using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;
using NServiceBus.Transport.SQLServer;

class Program
{
    static async Task Main()
    {
        LogManager.Use<NLogFactory>();
        NLog.LogManager.Configuration.DefaultCultureInfo = CultureInfo.InvariantCulture;

        Console.Title = "Samples.SqlServer.SimpleSender";
        var endpointConfiguration = new EndpointConfiguration("Samples.SqlServer.SimpleSender");
        endpointConfiguration.SendFailedMessagesTo("error");
        endpointConfiguration.UsePersistence<InMemoryPersistence>();
        endpointConfiguration.EnableInstallers();

        var recoverability = endpointConfiguration.Recoverability();
        recoverability.Delayed(settings => settings.NumberOfRetries(3).TimeIncrease(TimeSpan.FromSeconds(10)));
        recoverability.Immediate(s => s.NumberOfRetries(0));

        var transport = endpointConfiguration.UseTransport<SqlServerTransport>();
        var connection = @"Data Source=.;Database=SqlServerSimple;Integrated Security=True;Max Pool Size=100";
        transport.ConnectionString(connection);
        transport.Transactions(TransportTransactionMode.SendsAtomicWithReceive);

        var delayedDeliverySettings = transport.UseNativeDelayedDelivery();
        //delayedDeliverySettings.ProcessingInterval(TimeSpan.MaxValue);

        SqlHelper.EnsureDatabaseExists(connection);
        var endpointInstance = await Endpoint.Start(endpointConfiguration)
            .ConfigureAwait(false);

        var sessionCounter = 0;
        var tasks = new List<Task>();
        ConsoleKeyInfo k;
        while ((k = Console.ReadKey()).Key != ConsoleKey.Escape)
        {
            sessionCounter++;
            Console.WriteLine(k.KeyChar);
            var count = int.Parse(k.KeyChar.ToString());

            for (int i = 0; i < count; i++)
            {
                var options = new SendOptions();
                options.SetDestination("Samples.SqlServer.SimpleReceiver");
                options.SetMessageId($"{sessionCounter}/{i}");
                tasks.Add(endpointInstance.Send(new MyMessage(), options));
            }

            await Task.WhenAll(tasks)
                .ConfigureAwait(false);

            tasks.Clear();
            Console.WriteLine("Press ESC key to exit");
        }

        await endpointInstance.Stop()
            .ConfigureAwait(false);
    }
}