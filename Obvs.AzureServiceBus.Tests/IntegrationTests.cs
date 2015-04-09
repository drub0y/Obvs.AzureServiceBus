using System;
using Obvs.AzureServiceBus.Configuration;
using Obvs.Serialization.Json;
using Obvs.Types;
using Xunit;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using FluentAssertions;

namespace Obvs.AzureServiceBus.Tests
{
    public class IntegrationTests
    {
        [Fact]
        public async Task Test()
        {
            IServiceBus serviceBus = ServiceBus.Configure()
                .WithAzureServiceBusEndpoint<ITestMessage>()
                .Named("Test")
                .WithConnectionString("TODO: load this from a JSON file that is not checked into the repo")
                .UsingQueueFor<ICommand>("obvs-test-commands")
                .UsingQueueFor<IRequest>("obvs-test-requests")
                .UsingQueueFor<IResponse>("obvs-test-responses")
                .UsingTopicFor<IEvent>("obvs-test-events")
                .UsingSubscriptionFor<IEvent>("obvs-test-events/subscription")
                .SerializedWith(new JsonMessageSerializer(), new JsonMessageDeserializerFactory())
                .FilterMessageTypeAssemblies("Obvs.AzureServiceBus.Tests")
                .AsClientAndServer()
                .Create();

            await serviceBus.SendAsync(new TestCommand
            {
                Data = "Hello Azure SB!"
            });

            TestCommand command = await serviceBus.Commands.Timeout(TimeSpan.FromSeconds(5)).OfType<TestCommand>().FirstOrDefaultAsync();

            command.Should().NotBeNull();
        }
    }

    public interface ITestMessage : IMessage
    {
    }

    public class TestCommand : ITestMessage, ICommand
    {
        public string Data
        {
            get;
            set;
        }
    }
}
