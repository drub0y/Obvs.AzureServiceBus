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
            var serviceBus = ServiceBus<SampleMessage, SampleCommand, SampleEvent, SampleRequest, SampleResponse>.Configure()
                .WithAzureServiceBusEndpoint()
                .Named("Sample Message Bus")
                .WithConnectionString(Program.GetConnectionString())
                .UsingQueueFor<SampleCommand>(BuildMessagingEntityName("commands"), MessageReceiveMode.PeekLock, MessagingEntityCreationOptions.CreateIfDoesntExist | MessagingEntityCreationOptions.CreateAsTemporary | MessagingEntityCreationOptions.RecreateExistingTemporary)
                .UsingTopicFor<SampleEvent>(BuildMessagingEntityName("events"), MessagingEntityCreationOptions.CreateIfDoesntExist | MessagingEntityCreationOptions.CreateAsTemporary | MessagingEntityCreationOptions.RecreateExistingTemporary)
                .UsingSubscriptionFor<SampleEvent>(BuildMessagingEntityName("events"), "sample-subscription1", MessageReceiveMode.PeekLock, MessagingEntityCreationOptions.CreateIfDoesntExist | MessagingEntityCreationOptions.CreateAsTemporary | MessagingEntityCreationOptions.RecreateExistingTemporary)
                .SerializedAsJson()
                .FilterMessageTypeAssemblies(assembly => assembly == typeof(Program).Assembly, t => typeof(SampleMessage).IsAssignableFrom(t))
                .AsClientAndServer()
                .CreateServiceBus();

            Random commandProcessingChaosRandom = new Random();

            IDisposable commandsSubscription = serviceBus.Commands
                .SubscribeOn(TaskPoolScheduler.Default)
                .OfType<SampleCommand>()
                .SelectMany(async c =>
                {
                    Console.WriteLine("Got command: CommandId={0};DeliveryCount={1}", c.CommandId, c.GetIncomingMessageProperties().DeliveryCount);

                    // 90% of the time just complete the command successfully, 10% of the time simulate a failure so the message will be received again
                    if(commandProcessingChaosRandom.Next(1, 100) < 90)
                    {
                        await c.GetPeekLockControl().CompleteAsync();

                        await serviceBus.PublishAsync(new SampleEvent
                        {
                            EventId = "EVENT:" + c.CommandId,
                        });
                    }
                    else
                    {
                        Console.WriteLine("Simulating failure to call CompleteAsync, command with CommandId={0} will be received again.", c.CommandId);

                        await c.GetPeekLockControl().AbandonAsync();
                    }

                    return c;
                })
                .Subscribe();

            IDisposable commandSenderSubscription = null;
            IDisposable eventsSubscription = null;

            Console.WriteLine("'X' - stop...");
            Console.WriteLine("'C' - start/stop sending of commands");
            Console.WriteLine("'E' - attach/detach event listener");

            bool shouldStop = false;


            do
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);

                switch(keyInfo.Key)
                {
                    case ConsoleKey.C:
                        if(commandSenderSubscription == null)
                        {
                            commandSenderSubscription = Observable.Interval(TimeSpan.FromMilliseconds(500))
                                .SubscribeOn(TaskPoolScheduler.Default)
                                .Subscribe(async l =>
                                {
                                    string commandId = Guid.NewGuid().ToString("D");

                                    Console.WriteLine("Sending command {0}...", commandId);

                                    await serviceBus.SendAsync(new SampleCommand
                                        {
                                            CommandId = commandId
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
                                .SelectMany(async e =>
                                {
                                    Console.WriteLine("Got event: {0}", e.EventId);

                                    await e.GetPeekLockControl().CompleteAsync();

                                    return e;
                                })
                                .Subscribe();
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
