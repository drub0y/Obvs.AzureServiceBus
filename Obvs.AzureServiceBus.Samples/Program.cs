using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Obvs.AzureServiceBus.Configuration;
using Obvs.Serialization.Json.Configuration;
using Obvs.Types;

namespace Obvs.AzureServiceBus.Samples
{
    class Program
    {
        private const string MessagingEntityNameFormat = "obvs-azuresb-samples--{0}";

        static void Main(string[] args)
        {
            var serviceBus = ServiceBus.Configure()
                .WithAzureServiceBusEndpoint<SampleMessage>()
                .Named("Sample Message Bus")
                .WithConnectionString(Program.GetConnectionString())
                .UsingQueueFor<ICommand>(BuildMessagingEntityName("commands"), MessagingEntityCreationOptions.CreateIfDoesntExist | MessagingEntityCreationOptions.CreateAsTemporary | MessagingEntityCreationOptions.RecreateExistingTemporary)
                .UsingTopicFor<IEvent>(BuildMessagingEntityName("events"), MessagingEntityCreationOptions.CreateIfDoesntExist | MessagingEntityCreationOptions.CreateAsTemporary | MessagingEntityCreationOptions.RecreateExistingTemporary)
                .UsingSubscriptionFor<IEvent>(BuildMessagingEntityName("events"), "sample-subscription1", MessagingEntityCreationOptions.CreateIfDoesntExist | MessagingEntityCreationOptions.CreateAsTemporary | MessagingEntityCreationOptions.RecreateExistingTemporary)
                .SerializedAsJson()
                .FilterMessageTypeAssemblies(assembly => assembly == typeof(Program).Assembly)
                .AsClientAndServer()
                .UsingDebugLogging(e => true)
                .CreateServiceBus();

            IDisposable commandsSubscription = serviceBus.Commands
                .SubscribeOn(TaskPoolScheduler.Default)
                .OfType<SampleCommand>()
                .Subscribe(async c =>
                {
                    Console.WriteLine("Got command: {0}", c.CommandId);

                    await serviceBus.PublishAsync(new SampleEvent
                    {
                        EventId = "EVENT:" + c.CommandId,
                    });
                });

            IDisposable commandSenderSubscription = null;
            IDisposable eventsSubscription = null;

            Console.WriteLine("'X' - stop...");
            Console.WriteLine("'C' - start/stop sending of commands");
            Console.WriteLine("'E' - attach/detach event listener");

            bool shouldStop = false;
            Random random = new Random();

            do
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);

                switch(keyInfo.Key)
                {
                    case ConsoleKey.C:
                        if(commandSenderSubscription == null)
                        {
                            commandSenderSubscription = Observable.Interval(TimeSpan.FromMilliseconds(random.Next(250, 1000)))
                                .SubscribeOn(TaskPoolScheduler.Default)
                                .Subscribe(async l =>
                                {
                                    Console.WriteLine("Sending command...");
                                    
                                    await serviceBus.SendAsync(new SampleCommand
                                        {
                                            CommandId = Guid.NewGuid().ToString("D")
                                        });
                                });
                        }
                        else
                        {
                            commandSenderSubscription.Dispose();
                            commandSenderSubscription = null;
                        }

                        break;

                    case ConsoleKey.E:
                        if(eventsSubscription == null)
                        {
                            Console.WriteLine("Starting event listener...");

                            eventsSubscription = serviceBus.Events
                                .SubscribeOn(TaskPoolScheduler.Default)
                                .OfType<SampleEvent>()
                                .Subscribe(e =>
                                {
                                    Console.WriteLine("Got event: {0}", e.EventId);
                                });
                        }
                        else
                        {
                            Console.WriteLine("Stopping event listener!");

                            eventsSubscription.Dispose();
                            eventsSubscription = null;
                        }

                        break;

                    case ConsoleKey.X:
                        shouldStop = true;

                        break;
                }
            } while(!shouldStop);

            Console.ReadKey(true);
            Console.WriteLine("Shutting down...");

            if(commandSenderSubscription != null)
            {
                commandSenderSubscription.Dispose();
            }

            if(eventsSubscription != null)
            {
                eventsSubscription.Dispose();
            }

            commandsSubscription.Dispose();

            ((IDisposable)serviceBus).Dispose();

            Console.WriteLine("All subscriptions and Service Bus disposed, should have stopped receiving messages.");
            Console.WriteLine("Hit any key to exit!");
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
