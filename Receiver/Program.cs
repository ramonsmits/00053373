using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using NServiceBus;
using NServiceBus.Logging;
using NServiceBus.Transport.SQLServer;

class Program
{
    static async Task Main()
    {
        Console.Title = "Samples.SqlServer.SimpleReceiver";
        string[] args = Environment.GetCommandLineArgs();
        var childs = new List<Process>();
        if (args.Length > 1)
        {
            var instances = int.Parse(args[1]);
            for (int i = 0; i < instances; i++)
            {
                var pi = new ProcessStartInfo(args[0]);
                childs.Add(Process.Start(pi));
            }
            Console.Title = "Master";
        }

        LogManager.Use<NLogFactory>();
        NLog.LogManager.Configuration.DefaultCultureInfo = CultureInfo.InvariantCulture;

        /*
NServiceBus version=“6.4.3” targetFramework=“net452”
NServiceBus.SqlServer version=“3.1.3” targetFramework=“net452”
NServiceBus.Persistence.Sql version=“3.0.3” targetFramework=“net452”
*/
        var endpointConfiguration = new EndpointConfiguration("Samples.SqlServer.SimpleReceiver");
        endpointConfiguration.SendFailedMessagesTo("error");
        endpointConfiguration.UsePersistence<InMemoryPersistence>();
        endpointConfiguration.EnableInstallers();

        var recoverability = endpointConfiguration.Recoverability();
        recoverability.Delayed(settings => settings.NumberOfRetries(3).TimeIncrease(TimeSpan.FromSeconds(3)));
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
        Console.WriteLine("Press any key to exit");
        Console.WriteLine("Waiting for message from the Sender");
        Console.ReadKey();
        await endpointInstance.Stop()
            .ConfigureAwait(false);

        foreach (var p in childs) if (!p.HasExited) p.Kill();
    }
}