using System;
using Obvs.AzureServiceBus.Configuration;
using Obvs.Serialization.Json.Configuration;
using Obvs.Types;
using Xunit;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace Obvs.AzureServiceBus.Tests
{
    public class IntegrationTests
    {
        private static readonly string ServiceBusConnectionString = GetServiceBusConnectionString();
        
        [Fact]
        public async Task Test()
        {
            IServiceBus serviceBus = ServiceBus.Configure()
                .WithAzureServiceBusEndpoint<TestMessage>()
                .Named("Test")
                .WithConnectionString(IntegrationTests.ServiceBusConnectionString)
                .UsingQueueFor<ICommand>("obvs-test-commands", MessageReceiveMode.ReceiveAndDelete, MessagingEntityCreationOptions.CreateIfDoesntExist | MessagingEntityCreationOptions.CreateAsTemporary | MessagingEntityCreationOptions.RecreateExistingTemporary)
                .UsingQueueFor<IRequest>("obvs-test-requests", MessageReceiveMode.ReceiveAndDelete, MessagingEntityCreationOptions.CreateIfDoesntExist | MessagingEntityCreationOptions.CreateAsTemporary | MessagingEntityCreationOptions.RecreateExistingTemporary)
                .UsingQueueFor<IResponse>("obvs-test-responses", MessageReceiveMode.ReceiveAndDelete, MessagingEntityCreationOptions.CreateIfDoesntExist | MessagingEntityCreationOptions.CreateAsTemporary | MessagingEntityCreationOptions.RecreateExistingTemporary)
                .UsingTopicFor<IEvent>("obvs-test-events", MessagingEntityCreationOptions.CreateIfDoesntExist | MessagingEntityCreationOptions.CreateAsTemporary | MessagingEntityCreationOptions.RecreateExistingTemporary)
                .UsingSubscriptionFor<IEvent>("obvs-test-events", "obvs-test-subscription", MessageReceiveMode.ReceiveAndDelete, MessagingEntityCreationOptions.CreateIfDoesntExist | MessagingEntityCreationOptions.CreateAsTemporary | MessagingEntityCreationOptions.RecreateExistingTemporary)
                .SerializedAsJson()
                .FilterMessageTypeAssemblies("Obvs.AzureServiceBus.Tests")
                .AsClientAndServer()
                .Create();

            string commandId = Guid.NewGuid().ToString("n");

            await serviceBus.SendAsync(new TestCommand
            {
                Id = commandId,
                Data = "Hello Azure SB!"
            });

            TestCommand command = await serviceBus.Commands
                .OfType<TestCommand>()
                .Where(c => c.Id == commandId)
                .Timeout(TimeSpan.FromSeconds(5))
                .FirstOrDefaultAsync();

            command.Should().NotBeNull();
        }

        private static string GetServiceBusConnectionString()
        {
            string result = Environment.GetEnvironmentVariable("Obvs.AzureServiceBus.Tests.IntegrationTests.ServiceBusConnectionString");

            if(string.IsNullOrWhiteSpace(result))
            {
                try
                {
                    dynamic configuration = new JsonSerializer().Deserialize(new JsonTextReader(new StreamReader(@"..\..\..\IntegrationTestsConfig.json")));

                    result = configuration.ServiceBusConnectionString;
                }
                catch(FileNotFoundException)
                {
                    Debug.WriteLine("Didn't find JSON configuration file.");
                }
            }

            if(string.IsNullOrWhiteSpace(result))
            {
                throw new Exception("No service bus connection string has been configured.");
            }

            return result;
        }
    }

    public class TestMessage : IMessage
    {
        public string Id
        {
            get;
            set;
        }
    }

    public class TestCommand : TestMessage, ICommand
    {
        public string Data
        {
            get;
            set;
        }
    }
}
