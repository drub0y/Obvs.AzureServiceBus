using System;
using FluentAssertions;
using Microsoft.ServiceBus;
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
        public class NamespaceConfigurations
        {
            [Fact]
            public void ConfigureAzureServiceBusEndpointWithNullConnectionStringThrows()
            {
                Action action = () => ServiceBus.Configure()
                    .WithAzureServiceBusEndpoint<ITestMessage>()
                    .Named("Test Service Bus")
                    .WithConnectionString(null);
            
                action.ShouldThrow<ArgumentNullException>();
            }

            [Fact]
            public void ConfigureAzureServiceBusEndpointWithNullINamespaceManagerThrows()
            {
                Action action = () => ServiceBus.Configure()
                    .WithAzureServiceBusEndpoint<ITestMessage>()
                    .Named("Test Service Bus")
                    .WithNamespaceManager((INamespaceManager)null);

                action.ShouldThrow<ArgumentNullException>();
            }

            [Fact]
            public void ConfigureAzureServiceBusEndpointWithNullNamespaceManagerThrows()
            {
                Action action = () => ServiceBus.Configure()
                    .WithAzureServiceBusEndpoint<ITestMessage>()
                    .Named("Test Service Bus")
                    .WithNamespaceManager((NamespaceManager)null);

                action.ShouldThrow<ArgumentNullException>();
            }
        }

        public class MessageTypeConfigurations
        {
            private readonly Mock<INamespaceManager> _mockNamespaceManager;
            private readonly Mock<IMessagingFactory> _mockMessagingFactory;
            private readonly Mock<IMessageSerializer> _mockMessageSerializer;
            private readonly Mock<IMessageDeserializerFactory> _mockMessageDeserializerFactory;

            public MessageTypeConfigurations()
            {
                _mockNamespaceManager = new Mock<INamespaceManager>();
                _mockNamespaceManager.Setup(nsm => nsm.QueueExists(It.IsAny<string>()))
                    .Returns(true);
                _mockNamespaceManager.Setup(nsm => nsm.TopicExists(It.IsAny<string>()))
                    .Returns(true);
                _mockNamespaceManager.Setup(nsm => nsm.SubscriptionExists(It.IsAny<string>(), It.IsAny<string>()))
                    .Returns(true);

                _mockMessagingFactory = new Mock<IMessagingFactory>();

                _mockMessagingFactory.Setup(mf => mf.CreateMessageReceiver(It.IsAny<string>()))
                    .Returns(new Mock<IMessageReceiver>().Object);
                _mockMessagingFactory.Setup(mf => mf.CreateMessageSender(It.IsAny<string>()))
                    .Returns(new Mock<IMessageSender>().Object);

                _mockMessageSerializer = new Mock<IMessageSerializer>();
                _mockMessageDeserializerFactory = new Mock<IMessageDeserializerFactory>();
            }

            [Fact]
            public void ConfigureNoMessageTypesShouldThrow()
            {
                Action action = () => ServiceBus.Configure()
                    .WithAzureServiceBusEndpoint<ITestMessage>()
                    .Named("Test Service Bus")
                    .WithNamespaceManager(_mockNamespaceManager.Object)
                    .WithMessagingFactory(_mockMessagingFactory.Object)
                    .SerializedWith(_mockMessageSerializer.Object, _mockMessageDeserializerFactory.Object)
                    .AsClientAndServer()
                    .Create();

                action.ShouldThrow<ArgumentException>()
                    .And.ParamName.Should().Be("messageTypePathMappings");
            }

            [Fact]
            public void ConfigureSameMessageTypeForSameRoleMoreThanOnceShouldThrow()
            {
                Action action = () => ServiceBus.Configure()
                    .WithAzureServiceBusEndpoint<ITestMessage>()
                    .Named("Test Service Bus")
                    .WithNamespaceManager(_mockNamespaceManager.Object)
                    .WithMessagingFactory(_mockMessagingFactory.Object)
                    .UsingQueueFor<ICommand>("commands")
                    .UsingQueueFor<ICommand>("commandsAgain")
                    .SerializedWith(_mockMessageSerializer.Object, _mockMessageDeserializerFactory.Object)
                    .AsClientAndServer()
                    .Create();

                action.ShouldThrow<MoreThanOneMappingExistsForMessageTypeException>()
                    .And.MessageType.Should().Be(typeof(ICommand));
            }

            [Fact]
            public void ConfigureCommandMessageTypeOnlyShouldBeAbleToSendReceiveCommands()
            {
                IServiceBus serviceBus = ServiceBus.Configure()
                    .WithAzureServiceBusEndpoint<ITestMessage>()
                    .Named("Test Service Bus")
                    .WithNamespaceManager(_mockNamespaceManager.Object)
                    .WithMessagingFactory(_mockMessagingFactory.Object)
                    .UsingQueueFor<ICommand>("commands")
                    .SerializedWith(_mockMessageSerializer.Object, _mockMessageDeserializerFactory.Object)
                    .AsClientAndServer()
                    .Create();

                serviceBus.Should().NotBeNull();

                serviceBus.SendAsync(new TestCommand());
            }

            [Fact]
            public void SendingACommandWhenNotConfiguredAsAMessageTypeShouldThrow()
            {
                IServiceBus serviceBus = ServiceBus.Configure()
                    .WithAzureServiceBusEndpoint<ITestMessage>()
                    .Named("Test Service Bus")
                    .WithNamespaceManager(_mockNamespaceManager.Object)
                    .WithMessagingFactory(_mockMessagingFactory.Object)
                    .UsingQueueFor<IEvent>("events")
                    .SerializedWith(_mockMessageSerializer.Object, _mockMessageDeserializerFactory.Object)
                    .AsClientAndServer()
                    .Create();

                serviceBus.Should().NotBeNull();

                Action action = () => serviceBus.SendAsync(new TestCommand()).Wait();

                action.ShouldThrow<InvalidOperationException>();
            }

            [Fact]
            public void PublishingAnEventWhenNotConfiguredAsAMessageTypeShouldThrow()
            {
                IServiceBus serviceBus = ServiceBus.Configure()
                    .WithAzureServiceBusEndpoint<ITestMessage>()
                    .Named("Test Service Bus")
                    .WithNamespaceManager(_mockNamespaceManager.Object)
                    .WithMessagingFactory(_mockMessagingFactory.Object)
                    .UsingQueueFor<ICommand>("commands")
                    .SerializedWith(_mockMessageSerializer.Object, _mockMessageDeserializerFactory.Object)
                    .AsClientAndServer()
                    .Create();

                serviceBus.Should().NotBeNull();

                Action action = () => serviceBus.PublishAsync(new TestEvent()).Wait();

                action.ShouldThrow<InvalidOperationException>();
            }

            [Fact]
            public void ConfigureAllMessageTypes()
            {
                IServiceBus serviceBus = ServiceBus.Configure()
                    .WithAzureServiceBusEndpoint<ITestMessage>()
                        .Named("Test Service Bus")
                        .WithNamespaceManager(_mockNamespaceManager.Object)
                        .WithMessagingFactory(_mockMessagingFactory.Object)
                        .UsingQueueFor<ICommand>("commands")
                        .UsingQueueFor<IRequest>("requests")
                        .UsingQueueFor<IResponse>("responses")
                        .UsingTopicFor<IEvent>("events")
                        .UsingSubscriptionFor<IEvent>("events", "my-event-subscription")
                        .SerializedWith(_mockMessageSerializer.Object, _mockMessageDeserializerFactory.Object)
                        .FilterMessageTypeAssemblies("Obvs.AzureServiceBus.Tests")
                        .AsClientAndServer()
                    .Create();

                serviceBus.Should().NotBeNull();

                _mockNamespaceManager.Verify(nsm => nsm.QueueExists("commands"), Times.Once());
                _mockNamespaceManager.Verify(nsm => nsm.QueueExists("requests"), Times.Once());
                _mockNamespaceManager.Verify(nsm => nsm.QueueExists("responses"), Times.Once());
                _mockNamespaceManager.Verify(nsm => nsm.TopicExists("events"), Times.Once());
                _mockNamespaceManager.Verify(nsm => nsm.SubscriptionExists("events", "my-event-subscription"), Times.Once());

                _mockMessagingFactory.Verify(mf => mf.CreateMessageSender("commands"), Times.Once());
                _mockMessagingFactory.Verify(mf => mf.CreateMessageReceiver("commands"), Times.Once());

                _mockMessagingFactory.Verify(mf => mf.CreateMessageSender("requests"), Times.Once());
                _mockMessagingFactory.Verify(mf => mf.CreateMessageReceiver("requests"), Times.Once());

                _mockMessagingFactory.Verify(mf => mf.CreateMessageSender("responses"), Times.Once());
                _mockMessagingFactory.Verify(mf => mf.CreateMessageReceiver("responses"), Times.Once());

                _mockMessagingFactory.Verify(mf => mf.CreateMessageSender("events"), Times.Once());
                _mockMessagingFactory.Verify(mf => mf.CreateMessageReceiver("events"), Times.Never);

                _mockMessagingFactory.Verify(mf => mf.CreateMessageSender("events/subscriptions/my-event-subscription"), Times.Never);
                _mockMessagingFactory.Verify(mf => mf.CreateMessageReceiver("events/subscriptions/my-event-subscription"), Times.Once());
            }
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
