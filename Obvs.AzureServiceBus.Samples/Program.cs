using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Obvs.AzureServiceBus.Configuration;
using Obvs.Serialization.Json;
using Obvs.Types;

namespace Obvs.AzureServiceBus.Samples
{
    class Program
    {
        private const string MessagingEntityNameFormat = "obvs-azuresb-samples--{0}";
        
        static void Main(string[] args)
        {
            IServiceBus serviceBus = ServiceBus.Configure()
                .WithAzureServiceBusEndpoint<ISampleMessage>()
                .Named("Sample Message Bus")
                .WithConnectionString(Program.GetConnectionString())
                .UsingTemporaryQueueFor<ICommand>(BuildMessagingEntityName("commands"))
                //.UsingTemporaryQueueFor<IRequest>(BuildMessagingEntityName("requests"))
                //.UsingTemporaryQueueFor<IResponse>(BuildMessagingEntityName("responses"))
                //.UsingTemporaryQueueFor<IEvent>(BuildMessagingEntityName("events"))
                .SerializedWith(new JsonMessageSerializer(), new JsonMessageDeserializerFactory())
                .FilterMessageTypeAssemblies("Obvs.AzureServiceBus.Samples")
                .AsClientAndServer()
                .UsingDebugLogging()
                .Create();

            IDisposable comandsSubscription = serviceBus.Commands.Subscribe(c =>
                {
                    Console.WriteLine("Got a command!");   
                });

            IDisposable commandSenderSubscription = Observable.Interval(TimeSpan.FromSeconds(5))
                .Subscribe(l =>
                    {
                        serviceBus.SendAsync(new SampleComand());
                    });

            Console.WriteLine("Hit any key to stop...");
            Console.ReadKey(true);
            Console.WriteLine("Shutting down...");

            commandSenderSubscription.Dispose();
            comandsSubscription.Dispose();

            Console.WriteLine("Shut down completed. Hit any key to exit!");
            Console.ReadKey(true);
        }

        private static string BuildMessagingEntityName(string entityName)
        {
            return string.Format(MessagingEntityNameFormat, entityName);
        }

        private static string GetConnectionString()
        {
            string result = Environment.GetCommandLineArgs().Skip(1).FirstOrDefault();

            if(result == null)
            {
                result = Environment.GetEnvironmentVariable("AzureServiceBusSamples.ConnectionString");
            }

            if(string.IsNullOrWhiteSpace(result))
            {
                throw new InvalidOperationException("No connection string located!");
            }

            return result;
        }
    }
}
