using FluentAssertions;
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
                .Returns(new Mock<IMessageReceiver>().Object);
            mockMessagingFactory.Setup(mf => mf.CreateMessageSender(It.IsAny<string>()))
                .Returns(new Mock<IMessageSender>().Object);
            
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

            mockNamespaceManager.Verify(nsm => nsm.QueueExists("commands"), Times.Once());
            mockNamespaceManager.Verify(nsm => nsm.QueueExists("requests"), Times.Once());
            mockNamespaceManager.Verify(nsm => nsm.QueueExists("responses"), Times.Once());
            mockNamespaceManager.Verify(nsm => nsm.TopicExists("events"), Times.Once());
            mockNamespaceManager.Verify(nsm => nsm.SubscriptionExists("events", "my-event-subscription"), Times.Once());

            mockMessagingFactory.Verify(mf => mf.CreateMessageSender("commands"), Times.Once());
            mockMessagingFactory.Verify(mf => mf.CreateMessageReceiver("commands"), Times.Once());

            mockMessagingFactory.Verify(mf => mf.CreateMessageSender("requests"), Times.Once());
            mockMessagingFactory.Verify(mf => mf.CreateMessageReceiver("requests"), Times.Once());

            mockMessagingFactory.Verify(mf => mf.CreateMessageSender("responses"), Times.Once());
            mockMessagingFactory.Verify(mf => mf.CreateMessageReceiver("responses"), Times.Once());

            mockMessagingFactory.Verify(mf => mf.CreateMessageSender("events"), Times.Once());
            mockMessagingFactory.Verify(mf => mf.CreateMessageReceiver("events"), Times.Never);

            mockMessagingFactory.Verify(mf => mf.CreateMessageSender("events/subscriptions/my-event-subscription"), Times.Never);
            mockMessagingFactory.Verify(mf => mf.CreateMessageReceiver("events/subscriptions/my-event-subscription"), Times.Once());
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
