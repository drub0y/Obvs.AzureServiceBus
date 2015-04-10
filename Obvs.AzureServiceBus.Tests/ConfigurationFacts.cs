using FluentAssertions;
using Moq;
using Obvs.AzureServiceBus.Configuration;
using Obvs.Serialization;
using Obvs.Types;
using Xunit;

namespace Obvs.AzureServiceBus.Tests
{
    public class ConfigurationFacts
    {
        [Fact]
        public void ConfigureAzureServiceBusEndpoint()
        {
            IServiceBus serviceBus = ServiceBus.Configure()
                .WithAzureServiceBusEndpoint<ITestMessage>()
                    .Named("Test Service Bus")
                    .WithConnectionString("Endpoint=sb://obvs-azureservicebus-test.servicebus.windows.net/;SharedAccessKeyName=Test;SharedAccessKey=Test")
                    .UsingQueueFor<ICommand>("commands")
                    .UsingQueueFor<IRequest>("requests")
                    .UsingQueueFor<IResponse>("responses")
                    .UsingTopicFor<IEvent>("events")
                    .UsingSubscriptionFor<IEvent>("events/my-event-subscriptions")
                    .SerializedWith(Mock.Of<IMessageSerializer>(), Mock.Of<IMessageDeserializerFactory>())
                    .FilterMessageTypeAssemblies("Obvs.AzureServiceBus.Tests")
                    .AsClientAndServer()
                .Create();

            serviceBus.Should().NotBeNull();
        }

        public interface ITestMessage : IMessage
        {
        }

        public class TestEvent : ITestMessage, IEvent
        {

        }

        public class TestCommand : ITestMessage, ICommand
        {

        }

        public class TestRequest : ITestMessage, IRequest
        {
            public string RequestId
            {
                get;
                set;
            }

            public string RequesterId
            {
                get;
                set;
            }
        }

        public class TestResponse : ITestMessage, IResponse
        {
            public string RequestId
            {
                get;
                set;
            }

            public string RequesterId
            {
                get;
                set;
            }
        }
    }
}
