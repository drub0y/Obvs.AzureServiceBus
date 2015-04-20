using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
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
                .UsingTemporaryQueueFor<ICommand>(BuildMessagingEntityName("commands"), canDeleteIfAlreadyExists: true)
                //.UsingTemporaryQueueFor<IRequest>(BuildMessagingEntityName("requests"))
                //.UsingTemporaryQueueFor<IResponse>(BuildMessagingEntityName("responses"))
                //.UsingTemporaryQueueFor<IEvent>(BuildMessagingEntityName("events"))
                .SerializedWith(new JsonMessageSerializer(), new JsonMessageDeserializerFactory())
                .FilterMessageTypeAssemblies("Obvs.AzureServiceBus.Samples")
                .AsClientAndServer()
                .UsingDebugLogging()
                .Create();

            IDisposable commandsSubscription = serviceBus.Commands.SubscribeOn(TaskPoolScheduler.Default)
                .OfType<SampleCommand>()
                .Subscribe(c =>
                {
                    Console.WriteLine("Got command: {0}", c.CommandId);   
                });

            IDisposable commandSenderSubscription = Observable.Interval(TimeSpan.FromMilliseconds(250)).SubscribeOn(TaskPoolScheduler.Default)
                .Subscribe(l =>
                    {
                        Console.WriteLine("Sending command...");
                        serviceBus.SendAsync(new SampleCommand
                            {
                                CommandId = Guid.NewGuid().ToString("D")
                            });
                    });

            Console.WriteLine("Hit any key to stop...");
            Console.ReadKey(true);
            Console.WriteLine("Shutting down...");

            commandSenderSubscription.Dispose();
            commandsSubscription.Dispose();

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
                result = Environment.GetEnvironmentVariable("Obvs.AzureServiceBus.Samples.ConnectionString");
            }

            if(string.IsNullOrWhiteSpace(result))
            {
                throw new InvalidOperationException("No connection string located!");
            }

            return result;
        }
    }
}
