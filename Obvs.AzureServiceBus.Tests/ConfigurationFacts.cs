using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Moq;
using Obvs.AzureServiceBus.Configuration;
using Obvs.AzureServiceBus.Infrastructure;
using Obvs.Configuration;
using Obvs.MessageProperties;
using Obvs.Serialization;
using Obvs.Types;
using Xunit;

namespace Obvs.AzureServiceBus.Tests
{
    public class ConfigurationFacts
    {
        private readonly Mock<INamespaceManager> _mockNamespaceManager;
        private readonly Mock<IMessagingFactory> _mockMessagingFactory;
        private readonly Mock<IMessageSerializer> _mockMessageSerializer;
        private readonly Mock<IMessageDeserializerFactory> _mockMessageDeserializerFactory;

        public ConfigurationFacts()
        {
            _mockNamespaceManager = new Mock<INamespaceManager>();
            _mockNamespaceManager.Setup(nsm => nsm.QueueExists(It.IsAny<string>()))
                .Returns(true);
            _mockNamespaceManager.Setup(nsm => nsm.TopicExists(It.IsAny<string>()))
                .Returns(true);
            _mockNamespaceManager.Setup(nsm => nsm.SubscriptionExists(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);

            _mockMessagingFactory = new Mock<IMessagingFactory>();

            _mockMessagingFactory.Setup(mf => mf.CreateMessageReceiver(It.IsAny<Type>(), It.IsAny<string>(), It.IsAny<MessageReceiveMode>()))
                .Returns(new Mock<IMessageReceiver>().Object);
            _mockMessagingFactory.Setup(mf => mf.CreateMessageSender(It.IsAny<Type>(), It.IsAny<string>()))
                .Returns(new Mock<IMessageSender>().Object);

            _mockMessageSerializer = new Mock<IMessageSerializer>();
            _mockMessageDeserializerFactory = new Mock<IMessageDeserializerFactory>();
        }

        public class NamespaceConfigurationFacts : ConfigurationFacts
        {
            [Fact]
            public void ConfigureAzureServiceBusEndpointWithNullConnectionStringThrows()
            {
                Action action = () => ServiceBus.Configure()
                    .WithAzureServiceBusEndpoint<TestMessage>()
                    .Named("Test Service Bus")
                    .WithConnectionString(null);

                action.ShouldThrow<ArgumentNullException>();
            }

            [Fact]
            public void ConfigureAzureServiceBusEndpointWithNullINamespaceManagerThrows()
            {
                Action action = () => ServiceBus.Configure()
                    .WithAzureServiceBusEndpoint<TestMessage>()
                    .Named("Test Service Bus")
                    .WithNamespaceManager((INamespaceManager)null);

                action.ShouldThrow<ArgumentNullException>();
            }

            [Fact]
            public void ConfigureAzureServiceBusEndpointWithNullNamespaceManagerThrows()
            {
                Action action = () => ServiceBus.Configure()
                    .WithAzureServiceBusEndpoint<TestMessage>()
                    .Named("Test Service Bus")
                    .WithNamespaceManager((NamespaceManager)null);

                action.ShouldThrow<ArgumentNullException>();
            }
        }

        public class MessageTypeConfigurationFacts : ConfigurationFacts
        {
            [Fact]
            public void ConfigureNoMessageTypesShouldThrow()
            {
                Action action = () => ServiceBus.Configure()
                    .WithAzureServiceBusEndpoint<TestMessage>()
                    .Named("Test Service Bus")
                    .WithNamespaceManager(_mockNamespaceManager.Object)
                    .WithMessagingFactory(_mockMessagingFactory.Object)
                    .SerializedWith(_mockMessageSerializer.Object, _mockMessageDeserializerFactory.Object)
                    .AsClientAndServer()
                    .CreateServiceBus();

                action.ShouldThrow<ArgumentException>()
                    .And.ParamName.Should().Be("messageTypePathMappings");
            }

            [Fact]
            public void ConfigureSameMessageTypeForSameRoleMoreThanOnceShouldThrow()
            {
                Action action = () => ServiceBus.Configure()
                    .WithAzureServiceBusEndpoint<TestMessage>()
                    .Named("Test Service Bus")
                    .WithNamespaceManager(_mockNamespaceManager.Object)
                    .WithMessagingFactory(_mockMessagingFactory.Object)
                    .UsingQueueFor<ICommand>("commands")
                    .UsingQueueFor<ICommand>("commandsAgain");

                var exceptionAssertion = action.ShouldThrow<MappingAlreadyExistsForMessageTypeException>();

                exceptionAssertion.And.MessageType.Should().Be(typeof(ICommand));
                exceptionAssertion.And.EntityType.Should().Be(MessagingEntityType.Queue);
            }

            [Fact]
            public async Task ConfigureCommandMessageTypeOnlyShouldBeAbleToSendReceiveCommands()
            {
                IServiceBus<IMessage, ICommand, IEvent, IRequest, IResponse> serviceBus = ServiceBus.Configure()
                    .WithAzureServiceBusEndpoint<TestMessage>()
                    .Named("Test Service Bus")
                    .WithNamespaceManager(_mockNamespaceManager.Object)
                    .WithMessagingFactory(_mockMessagingFactory.Object)
                    .UsingQueueFor<ICommand>("commands")
                    .SerializedWith(_mockMessageSerializer.Object, _mockMessageDeserializerFactory.Object)
                    .AsClientAndServer()
                    .CreateServiceBus();

                serviceBus.Should().NotBeNull();

                await serviceBus.SendAsync(new TestCommand());
            }

            [Fact]
            public void SendingACommandWhenNotConfiguredAsAMessageTypeShouldThrow()
            {
                IServiceBus<IMessage, ICommand, IEvent, IRequest, IResponse> serviceBus = ServiceBus.Configure()
                    .WithAzureServiceBusEndpoint<TestMessage>()
                    .Named("Test Service Bus")
                    .WithNamespaceManager(_mockNamespaceManager.Object)
                    .WithMessagingFactory(_mockMessagingFactory.Object)
                    .UsingQueueFor<IEvent>("events")
                    .SerializedWith(_mockMessageSerializer.Object, _mockMessageDeserializerFactory.Object)
                    .AsClientAndServer()
                    .CreateServiceBus();

                serviceBus.Should().NotBeNull();

                Action action = () => serviceBus.SendAsync(new TestCommand()).Wait();

                action.ShouldThrow<InvalidOperationException>();
            }

            [Fact]
            public void PublishingAnEventWhenNotConfiguredAsAMessageTypeShouldThrow()
            {
                IServiceBus<IMessage, ICommand, IEvent, IRequest, IResponse> serviceBus = ServiceBus.Configure()
                    .WithAzureServiceBusEndpoint<TestMessage>()
                    .Named("Test Service Bus")
                    .WithNamespaceManager(_mockNamespaceManager.Object)
                    .WithMessagingFactory(_mockMessagingFactory.Object)
                    .UsingQueueFor<ICommand>("commands")
                    .SerializedWith(_mockMessageSerializer.Object, _mockMessageDeserializerFactory.Object)
                    .AsClientAndServer()
                    .CreateServiceBus();

                serviceBus.Should().NotBeNull();

                Action action = () => serviceBus.PublishAsync(new TestEvent()).Wait();

                action.ShouldThrow<InvalidOperationException>();
            }

            [Fact]
            public void ConfigureAllMessageTypes()
            {
                IServiceBus<IMessage, ICommand, IEvent, IRequest, IResponse> serviceBus = ServiceBus.Configure()
                    .WithAzureServiceBusEndpoint<TestMessage>()
                        .Named("Test Service Bus")
                        .WithNamespaceManager(_mockNamespaceManager.Object)
                        .WithMessagingFactory(_mockMessagingFactory.Object)
                        .UsingQueueFor<ICommand>("commands")
                        .UsingQueueFor<IRequest>("requests")
                        .UsingQueueFor<IResponse>("responses")
                        .UsingTopicFor<IEvent>("events")
                        .UsingSubscriptionFor<IEvent>("events", "my-event-subscription")
                        .SerializedWith(_mockMessageSerializer.Object, _mockMessageDeserializerFactory.Object)
                        .FilterMessageTypeAssemblies(assembly => assembly.GetName().Name == "Obvs.AzureServiceBus.Tests")
                        .AsClientAndServer()
                    .CreateServiceBus();

                serviceBus.Should().NotBeNull();

                _mockNamespaceManager.Verify(nsm => nsm.QueueExists("commands"), Times.Once());
                _mockNamespaceManager.Verify(nsm => nsm.QueueExists("requests"), Times.Once());
                _mockNamespaceManager.Verify(nsm => nsm.QueueExists("responses"), Times.Once());
                _mockNamespaceManager.Verify(nsm => nsm.TopicExists("events"), Times.Once());
                _mockNamespaceManager.Verify(nsm => nsm.SubscriptionExists("events", "my-event-subscription"), Times.Once());

                _mockMessagingFactory.Verify(mf => mf.CreateMessageSender(typeof(ICommand), "commands"), Times.Once());
                _mockMessagingFactory.Verify(mf => mf.CreateMessageReceiver(typeof(ICommand), "commands", MessageReceiveMode.PeekLock), Times.Once());

                _mockMessagingFactory.Verify(mf => mf.CreateMessageSender(typeof(IRequest), "requests"), Times.Once());
                _mockMessagingFactory.Verify(mf => mf.CreateMessageReceiver(typeof(IRequest), "requests", MessageReceiveMode.PeekLock), Times.Once());

                _mockMessagingFactory.Verify(mf => mf.CreateMessageSender(typeof(IResponse), "responses"), Times.Once());
                _mockMessagingFactory.Verify(mf => mf.CreateMessageReceiver(typeof(IResponse), "responses", MessageReceiveMode.PeekLock), Times.Once());

                _mockMessagingFactory.Verify(mf => mf.CreateMessageSender(typeof(IEvent), "events"), Times.Once());
                _mockMessagingFactory.Verify(mf => mf.CreateMessageReceiver(typeof(IEvent), "events", MessageReceiveMode.PeekLock), Times.Never);

                _mockMessagingFactory.Verify(mf => mf.CreateMessageSender(typeof(IEvent), "events/subscriptions/my-event-subscription"), Times.Never);
                _mockMessagingFactory.Verify(mf => mf.CreateMessageReceiver(typeof(IEvent), "events/subscriptions/my-event-subscription", MessageReceiveMode.PeekLock), Times.Once());
            }
        }

        public class ExistingMessagingEntityFacts : ConfigurationFacts
        {
            [Fact]
            public void UseExistingMessagingEntityThatDoesNotExistShouldThrow()
            {
                _mockNamespaceManager.Setup(nsm => nsm.QueueExists("commands"))
                    .Returns(false);

                Action action = () => ServiceBus.Configure()
                 .WithAzureServiceBusEndpoint<TestMessage>()
                 .Named("Test Service Bus")
                 .WithNamespaceManager(_mockNamespaceManager.Object)
                 .WithMessagingFactory(_mockMessagingFactory.Object)
                 .UsingQueueFor<ICommand>("commands")
                 .SerializedWith(_mockMessageSerializer.Object, _mockMessageDeserializerFactory.Object)
                 .AsClientAndServer()
                 .CreateServiceBus();

                var exceptionAssertion = action.ShouldThrow<MessagingEntityDoesNotAlreadyExistException>();

                exceptionAssertion.And.Path.Should().Be("commands");
                exceptionAssertion.And.MessagingEntityType.Should().Be(MessagingEntityType.Queue);
            }

            [Fact]
            public void UseExistingMessagingEntityShouldNotTryToCreateTheMessagingEntity()
            {
                _mockNamespaceManager.Setup(nsm => nsm.QueueExists("commands"))
                    .Returns(true);

                ServiceBus.Configure()
                 .WithAzureServiceBusEndpoint<TestMessage>()
                 .Named("Test Service Bus")
                 .WithNamespaceManager(_mockNamespaceManager.Object)
                 .WithMessagingFactory(_mockMessagingFactory.Object)
                 .UsingQueueFor<ICommand>("commands", MessagingEntityCreationOptions.CreateIfDoesntExist)
                 .SerializedWith(_mockMessageSerializer.Object, _mockMessageDeserializerFactory.Object)
                 .AsClientAndServer()
                 .CreateServiceBus();

                _mockNamespaceManager.Verify(nsm => nsm.QueueExists("commands"), Times.Once());
                _mockNamespaceManager.Verify(nsm => nsm.CreateQueue("commands"), Times.Never);
            }
        }

        public class TemporaryMessagingEntityFacts : ConfigurationFacts
        {
            [Fact]
            public void UseTemporaryMessagingEntityThatAlreadyExistsWithoutSpecifyingCanDeleteIfAlreadyExistsShouldThrow()
            {
                Action action = () => ServiceBus.Configure()
                    .WithAzureServiceBusEndpoint<TestMessage>()
                    .Named("Test Service Bus")
                    .WithNamespaceManager(_mockNamespaceManager.Object)
                    .WithMessagingFactory(_mockMessagingFactory.Object)
                    .UsingQueueFor<ICommand>("commands", MessagingEntityCreationOptions.CreateIfDoesntExist | MessagingEntityCreationOptions.CreateAsTemporary)
                    .SerializedWith(_mockMessageSerializer.Object, _mockMessageDeserializerFactory.Object)
                    .AsClientAndServer()
                    .CreateServiceBus();

                var exceptionAssertion = action.ShouldThrow<Obvs.AzureServiceBus.Configuration.MessagingEntityAlreadyExistsException>();

                exceptionAssertion.And.Path.Should().Be("commands");
                exceptionAssertion.And.MessagingEntityType.Should().Be(MessagingEntityType.Queue);
            }

            [Fact]
            public void UseTemporaryMessagingEntityThatAlreadyExiststSpecifyingRecreateOptionShouldRecreate()
            {
                ServiceBus.Configure()
                    .WithAzureServiceBusEndpoint<TestMessage>()
                    .Named("Test Service Bus")
                    .WithNamespaceManager(_mockNamespaceManager.Object)
                    .WithMessagingFactory(_mockMessagingFactory.Object)
                    .UsingQueueFor<ICommand>("commands", MessagingEntityCreationOptions.CreateIfDoesntExist | MessagingEntityCreationOptions.CreateAsTemporary | MessagingEntityCreationOptions.RecreateExistingTemporary)
                    .SerializedWith(_mockMessageSerializer.Object, _mockMessageDeserializerFactory.Object)
                    .AsClientAndServer()
                    .CreateServiceBus();

                _mockNamespaceManager.Verify(nsm => nsm.DeleteQueue("commands"), Times.Once);
                _mockNamespaceManager.Verify(nsm => nsm.CreateQueue("commands"), Times.Once);
            }

            [Fact]
            public void UseTemporarySubscriptionForTopicThatAlreadyExistsShouldCreateSubscription()
            {
                _mockNamespaceManager.Setup(nsm => nsm.TopicExists("events"))
                    .Returns(true);

                _mockNamespaceManager.Setup(nsm => nsm.SubscriptionExists("events", "test-subscription"))
                    .Returns(false);

                ServiceBus.Configure()
                    .WithAzureServiceBusEndpoint<TestMessage>()
                    .Named("Test Service Bus")
                    .WithNamespaceManager(_mockNamespaceManager.Object)
                    .WithMessagingFactory(_mockMessagingFactory.Object)
                    .UsingSubscriptionFor<IEvent>("events", "test-subscription", MessagingEntityCreationOptions.CreateIfDoesntExist | MessagingEntityCreationOptions.CreateAsTemporary | MessagingEntityCreationOptions.RecreateExistingTemporary)
                    .SerializedWith(_mockMessageSerializer.Object, _mockMessageDeserializerFactory.Object)
                    .AsClient()
                    .CreateServiceBusClient();

                _mockNamespaceManager.Verify(nsm => nsm.TopicExists("events"), Times.Once);
                _mockNamespaceManager.Verify(nsm => nsm.CreateSubscription("events", "test-subscription"), Times.Once);
            }

            [Fact]
            public void UseTemporarySubscriptionForTopicThatDoesntAlreadyExistThrows()
            {
                _mockNamespaceManager.Setup(nsm => nsm.TopicExists("events"))
                    .Returns(false);

                _mockNamespaceManager.Setup(nsm => nsm.SubscriptionExists("events", "test-subscription"))
                    .Returns(false);

                Action action = () => ServiceBus.Configure()
                    .WithAzureServiceBusEndpoint<TestMessage>()
                    .Named("Test Service Bus")
                    .WithNamespaceManager(_mockNamespaceManager.Object)
                    .WithMessagingFactory(_mockMessagingFactory.Object)
                    .UsingSubscriptionFor<IEvent>("events", "test-subscription", MessagingEntityCreationOptions.CreateIfDoesntExist | MessagingEntityCreationOptions.CreateAsTemporary)
                    .SerializedWith(_mockMessageSerializer.Object, _mockMessageDeserializerFactory.Object)
                    .AsClient()
                    .CreateServiceBusClient();

                var exceptionAssertion = action.ShouldThrow<MessagingEntityDoesNotAlreadyExistException>();

                exceptionAssertion.And.Path.Should().Be("events");
                exceptionAssertion.And.MessagingEntityType.Should().Be(MessagingEntityType.Topic);
            }

            [Fact]
            public void UseTemporarySubscriptionForTemporaryTopicShouldCreateTopicAndSubscription()
            {
                _mockNamespaceManager.Setup(nsm => nsm.TopicExists("events"))
                    .Returns(false);

                _mockNamespaceManager.Setup(nsm => nsm.SubscriptionExists("events", "test-subscription"))
                    .Returns(false);

                ServiceBus.Configure()
                    .WithAzureServiceBusEndpoint<TestMessage>()
                    .Named("Test Service Bus")
                    .WithNamespaceManager(_mockNamespaceManager.Object)
                    .WithMessagingFactory(_mockMessagingFactory.Object)
                    .UsingTopicFor<IEvent>("events", MessagingEntityCreationOptions.CreateIfDoesntExist | MessagingEntityCreationOptions.CreateAsTemporary)
                    .UsingSubscriptionFor<IEvent>("events", "test-subscription", MessagingEntityCreationOptions.CreateIfDoesntExist | MessagingEntityCreationOptions.CreateAsTemporary)
                    .SerializedWith(_mockMessageSerializer.Object, _mockMessageDeserializerFactory.Object)
                    .AsClient()
                    .CreateServiceBusClient();

                _mockNamespaceManager.Verify(nsm => nsm.CreateTopic("events"), Times.Once);
                _mockNamespaceManager.Verify(nsm => nsm.CreateSubscription("events", "test-subscription"), Times.Once);
            }

            [Fact]
            public void UseTemporarySubscriptionThatAlreadyExistsShouldRecreateSubscription()
            {
                _mockNamespaceManager.Setup(nsm => nsm.TopicExists("events"))
                    .Returns(true);

                _mockNamespaceManager.Setup(nsm => nsm.SubscriptionExists("events", "test-subscription"))
                    .Returns(true);

                ServiceBus.Configure()
                    .WithAzureServiceBusEndpoint<TestMessage>()
                    .Named("Test Service Bus")
                    .WithNamespaceManager(_mockNamespaceManager.Object)
                    .WithMessagingFactory(_mockMessagingFactory.Object)
                    .UsingSubscriptionFor<IEvent>("events", "test-subscription", MessagingEntityCreationOptions.CreateIfDoesntExist | MessagingEntityCreationOptions.CreateAsTemporary | MessagingEntityCreationOptions.RecreateExistingTemporary)
                    .SerializedWith(_mockMessageSerializer.Object, _mockMessageDeserializerFactory.Object)
                    .AsClient()
                    .CreateServiceBusClient();

                _mockNamespaceManager.Verify(nsm => nsm.DeleteSubscription("events", "test-subscription"), Times.Once);
                _mockNamespaceManager.Verify(nsm => nsm.CreateSubscription("events", "test-subscription"), Times.Once);
            }
        }

        public class PropertyProviderConfigurationFacts : ConfigurationFacts
        {
            private class NotATestMessage : IMessage
            {

            }

            [Fact]
            public void ConfiguringANullProviderInstanceThrows()
            {
                Action action = () => ServiceBus.Configure()
                    .WithAzureServiceBusEndpoint<TestMessage>()
                    .Named("Test Service Bus")
                    .WithNamespaceManager(_mockNamespaceManager.Object)
                    .WithMessagingFactory(_mockMessagingFactory.Object)
                    .UsingQueueFor<ICommand>("test")
                    .UsingMessagePropertyProviderFor<TestCommand>(null)
                    .SerializedWith(_mockMessageSerializer.Object, _mockMessageDeserializerFactory.Object)
                    .AsClientAndServer();

                action.ShouldThrow<ArgumentNullException>();
            }

            [Fact]
            public void ConfiguringAProviderForAMessageThatIsNotDerivedFromTheServiceMessageTypeThrows()
            {
                Action action = () => ServiceBus.Configure()
                    .WithAzureServiceBusEndpoint<TestMessage>()
                    .Named("Test Service Bus")
                    .WithNamespaceManager(_mockNamespaceManager.Object)
                    .WithMessagingFactory(_mockMessagingFactory.Object)
                    .UsingQueueFor<ICommand>("test")
                    .UsingMessagePropertyProviderFor<NotATestMessage>(new FuncMessagePropertyProvider<NotATestMessage>(c => null))
                    .SerializedWith(_mockMessageSerializer.Object, _mockMessageDeserializerFactory.Object)
                    .AsClientAndServer();

                action.ShouldThrow<ArgumentException>()
                    .And.ParamName.Should().Be("messagePropertyProvider");
            }

            [Fact]
            public void ConfigureProvidersForMultipleMessageTypes()
            {
                Action action = () => ServiceBus.Configure()
                    .WithAzureServiceBusEndpoint<TestMessage>()
                    .Named("Test Service Bus")
                    .WithNamespaceManager(_mockNamespaceManager.Object)
                    .WithMessagingFactory(_mockMessagingFactory.Object)
                    .UsingQueueFor<ICommand>("test")
                    .UsingMessagePropertyProviderFor<TestCommand>(new FuncMessagePropertyProvider<TestCommand>(tc => new KeyValuePair<string, object>("CommandProp", "CommandPropValue")))
                    .UsingMessagePropertyProviderFor<TestEvent>(new FuncMessagePropertyProvider<TestEvent>(tc => new KeyValuePair<string, object>("EventProp", "EventPropValue")))
                    .SerializedWith(_mockMessageSerializer.Object, _mockMessageDeserializerFactory.Object)
                    .AsClientAndServer();
            }

            public void ConfigureMultipleProvidersForSameMessageType()
            {
                Action action = () => ServiceBus.Configure()
                    .WithAzureServiceBusEndpoint<TestMessage>()
                    .Named("Test Service Bus")
                    .WithNamespaceManager(_mockNamespaceManager.Object)
                    .WithMessagingFactory(_mockMessagingFactory.Object)
                    .UsingQueueFor<ICommand>("test")
                    .UsingMessagePropertyProviderFor<TestCommand>(new FuncMessagePropertyProvider<TestCommand>(tc => new KeyValuePair<string, object>("CommandProp1", "CommandPropValue1")))
                    .UsingMessagePropertyProviderFor<TestCommand>(new FuncMessagePropertyProvider<TestCommand>(tc => new KeyValuePair<string, object>("CommandProp2", "CommandPropValue2")))
                    .SerializedWith(_mockMessageSerializer.Object, _mockMessageDeserializerFactory.Object)
                    .AsClientAndServer();
            }

            [Fact]
            public async Task EnsureProvidedPropertiesArePresent()
            {
                BrokeredMessage brokeredMessage = null;

                Mock<IMessageSender> mockMessageSender = new Mock<IMessageSender>();
                mockMessageSender.Setup(ms => ms.SendAsync(It.IsAny<BrokeredMessage>()))
                    .Callback<BrokeredMessage>(bm => brokeredMessage = bm)
                    .Returns(Task.FromResult(true));

                _mockMessagingFactory.Setup(mf => mf.CreateMessageSender(It.IsAny<Type>(), It.IsAny<string>()))
                    .Returns(mockMessageSender.Object);

                CompositeMessagePropertyProvider<TestCommand> propertyProvider = new CompositeMessagePropertyProvider<TestCommand>();
                propertyProvider.Providers.AddRange(
                        c => new KeyValuePair<string, object>("SomeProp", "SomeValue"),
                        c => new KeyValuePair<string, object>("SomeOtherProp", "SomeOtherValue"));

                FuncMessagePropertyProvider<TestCommand> propertyProvider2 = new FuncMessagePropertyProvider<TestCommand>(c => new KeyValuePair<string, object>("SomeThirdProp", "SomeThirdValue"));

                IServiceBus serviceBus = ServiceBus.Configure()
                    .WithAzureServiceBusEndpoint<TestMessage>()
                    .Named("Test Service Bus")
                    .WithNamespaceManager(_mockNamespaceManager.Object)
                    .WithMessagingFactory(_mockMessagingFactory.Object)
                    .UsingQueueFor<ICommand>("test")
                    .UsingMessagePropertyProviderFor<TestCommand>(propertyProvider)
                    .UsingMessagePropertyProviderFor<TestCommand>(propertyProvider2)
                    .SerializedWith(_mockMessageSerializer.Object, _mockMessageDeserializerFactory.Object)
                    .AsClientAndServer()
                    .Create();

                await serviceBus.SendAsync(new TestCommand());

                brokeredMessage.Should().NotBeNull();

                brokeredMessage.Properties.Should()
                    .Contain(new KeyValuePair<string, object>("SomeProp", "SomeValue"))
                    .And.Contain(new KeyValuePair<string, object>("SomeOtherProp", "SomeOtherValue"))
                    .And.Contain(new KeyValuePair<string, object>("SomeThirdProp", "SomeThirdValue"));
            }
        }

        public class TestMessage : IMessage
        {
        }

        public class TestEvent : TestMessage, IEvent
        {

        }

        public class TestCommand : TestMessage, ICommand
        {
            public string CommandId
            {
                get;
                set;
            }
        }

        public class TestRequest : TestMessage, IRequest
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

        public class TestResponse : TestMessage, IResponse
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
