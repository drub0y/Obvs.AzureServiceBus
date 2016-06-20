using System;
using System.Diagnostics;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using Obvs.AzureServiceBus.Configuration;
using Obvs.Serialization.Json.Configuration;
using Xunit;

namespace Obvs.AzureServiceBus.Tests
{
    public class IntegrationTests
    {
        private static readonly string ServiceBusConnectionString = GetServiceBusConnectionString();

        [Fact]
        public void SendMessageToNonExistentQueue()
        {
            var serviceBus = ServiceBus<TestMessage, TestCommand, TestEvent, TestRequest, TestResponse>.Configure()
                .WithAzureServiceBusEndpoint()
                .Named("Test")
                .WithConnectionString(IntegrationTests.ServiceBusConnectionString)
                .UsingQueueFor<TestCommand>("NON-EXISTENT-QUEUE", MessageReceiveMode.ReceiveAndDelete)
                .SerializedAsJson()
                .FilterMessageTypeAssemblies(a => a.GetName().Name == "Obvs.AzureServiceBus.Tests")
                .AsClientAndServer()
                .CreateServiceBus();

            Func<Task> action = async () => await serviceBus.SendAsync(new TestCommand
            {
                Id = "TestId",
                Data = "TestData"
            });
            
            action.ShouldThrow<Exception>();
        }

        [Fact]
        public void RecieveMessagesFromNonExistentQueue()
        {
            var serviceBus = ServiceBus<TestMessage, TestCommand, TestEvent, TestRequest, TestResponse>.Configure()
                .WithAzureServiceBusEndpoint()
                .Named("Test")
                .WithConnectionString(IntegrationTests.ServiceBusConnectionString)
                .UsingQueueFor<TestCommand>("NON-EXISTENT-QUEUE", MessageReceiveMode.ReceiveAndDelete)
                .SerializedAsJson()
                .FilterMessageTypeAssemblies(a => a.GetName().Name == "Obvs.AzureServiceBus.Tests")
                .AsClientAndServer()
                .CreateServiceBus();

            Func<Task> action = async () => await serviceBus.Commands.FirstOrDefaultAsync();

            action.ShouldThrow<Exception>();
        }

        [Fact]
        public async Task SendAndReceiveSingleCommand()
        {
            var serviceBus = ServiceBus<TestMessage, TestCommand, TestEvent, TestRequest, TestResponse>.Configure()
                .WithAzureServiceBusEndpoint()
                .Named("Test")
                .WithConnectionString(IntegrationTests.ServiceBusConnectionString)
                .UsingQueueFor<TestCommand>("obvs-test-commands", MessageReceiveMode.ReceiveAndDelete, MessagingEntityCreationOptions.CreateIfDoesntExist | MessagingEntityCreationOptions.CreateAsTemporary | MessagingEntityCreationOptions.RecreateExistingTemporary)
                .SerializedAsJson()
                .FilterMessageTypeAssemblies(a => a.GetName().Name == "Obvs.AzureServiceBus.Tests")
                .AsClientAndServer()
                .CreateServiceBus();

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

    public class TestMessage
    {
        public string Id
        {
            get;
            set;
        }
    }

    public class TestCommand : TestMessage
    {
        public string Data
        {
            get;
            set;
        }
    }

    public class TestEvent : TestMessage
    {

    }

    public class TestRequest : TestMessage
    {
    }

    public class TestResponse : TestMessage
    {
    }
}
