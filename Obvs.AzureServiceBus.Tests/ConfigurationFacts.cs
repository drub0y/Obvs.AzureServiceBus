using FluentAssertions;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Moq;
using Obvs.AzureServiceBus.Configuration;
using Obvs.AzureServiceBus.Infrastructure;
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
            Mock<INamespaceManager> mockNamespaceManager = new Mock<INamespaceManager>();
            mockNamespaceManager.Setup(nsm => nsm.QueueExists(It.IsAny<string>()))
                .Returns(true);
            mockNamespaceManager.Setup(nsm => nsm.TopicExists(It.IsAny<string>()))
                .Returns(true);
            mockNamespaceManager.Setup(nsm => nsm.SubscriptionExists(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);

            Mock<IMessagingFactory> mockMessagingFactory = new Mock<IMessagingFactory>();

            mockMessagingFactory.Setup(mf => mf.CreateMessageReceiver(It.IsAny<string>()))
                .Returns(new Mock<MessageReceiver>().Object);
            mockMessagingFactory.Setup(mf => mf.CreateMessageSender(It.IsAny<string>()))
                .Returns(new Mock<MessageSender>().Object);
            
            IServiceBus serviceBus = ServiceBus.Configure()
                .WithAzureServiceBusEndpoint<ITestMessage>()
                    .Named("Test Service Bus")
                    .WithNamespaceManager(mockNamespaceManager.Object)
                    .WithMessagingFactory(mockMessagingFactory.Object)
                    .UsingQueueFor<ICommand>("commands")
                    .UsingQueueFor<IRequest>("requests")
                    .UsingQueueFor<IResponse>("responses")
                    .UsingTopicFor<IEvent>("events")
                    .UsingSubscriptionFor<IEvent>("events", "my-event-subscription")
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
