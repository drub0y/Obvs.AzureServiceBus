using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.ServiceBus.Messaging;
using Moq;
using Obvs.AzureServiceBus.Configuration;
using Obvs.AzureServiceBus.Infrastructure;
using Obvs.MessageProperties;
using Obvs.Serialization;
using Obvs.Serialization.Json;
using Obvs.Types;
using Xunit;

namespace Obvs.AzureServiceBus.Tests
{
    public class EndpointProviderFacts
    {
        private readonly Func<Assembly, bool> _testAssemblyFilter = a => typeof(EndpointProviderFacts).Assembly.Equals(a);
        private readonly Func<Type, bool> _testMessageTypeFilter = t => typeof(TestMessage).IsAssignableFrom(t);

        private readonly List<MessageTypeMessagingEntityMappingDetails> _messageTypePathMappings;

        private readonly Mock<IMessageOutgoingPropertiesTable> _mockMessageOutgoingPropertiesTable;


        public EndpointProviderFacts()
        {
            _messageTypePathMappings = new List<MessageTypeMessagingEntityMappingDetails>
            {
                new MessageTypeMessagingEntityMappingDetails(typeof(TestCommand), "commands", MessagingEntityType.Queue),
                new MessageTypeMessagingEntityMappingDetails(typeof(TestEvent), "events", MessagingEntityType.Topic)
            };

            _mockMessageOutgoingPropertiesTable = new Mock<IMessageOutgoingPropertiesTable>();
        }

        public class ConstructorFacts : EndpointProviderFacts
        {
            [Fact]
            public void CreatingWithNullMessagingFactoryThrows()
            {
                Action action = () =>
                {
                    new AzureServiceBusEndpointProvider<TestMessage, TestMessage, TestCommand, TestEvent, TestRequest, TestResponse>("TestEndpoint", null, Mock.Of<IMessageSerializer>(), Mock.Of<IMessageDeserializerFactory>(), _messageTypePathMappings, _testAssemblyFilter, _testMessageTypeFilter, new MessagePropertyProviderManager<TestMessage>(), _mockMessageOutgoingPropertiesTable.Object);
                };

                action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("messagingFactory");
            }

            [Fact]
            public void CreatingWithNullMessageSerializerThrows()
            {
                Action action = () =>
                {
                    new AzureServiceBusEndpointProvider<TestMessage, TestMessage, TestCommand, TestEvent, TestRequest, TestResponse>("TestEndpoint", Mock.Of<IMessagingFactory>(), null, Mock.Of<IMessageDeserializerFactory>(), _messageTypePathMappings, _testAssemblyFilter, _testMessageTypeFilter, new MessagePropertyProviderManager<TestMessage>(), _mockMessageOutgoingPropertiesTable.Object);
                };

                action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("messageSerializer");
            }

            [Fact]
            public void CreatingWithNullMessageDeserializerFactoryThrows()
            {
                Action action = () =>
                {
                    new AzureServiceBusEndpointProvider<TestMessage, TestMessage, TestCommand, TestEvent, TestRequest, TestResponse>("TestEndpoint", Mock.Of<IMessagingFactory>(), Mock.Of<IMessageSerializer>(), null, _messageTypePathMappings, _testAssemblyFilter, _testMessageTypeFilter, new MessagePropertyProviderManager<TestMessage>(), _mockMessageOutgoingPropertiesTable.Object);
                };

                action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("messageDeserializerFactory");
            }

            [Fact]
            public void CreatingWithNullMessageTypePathMappingsThrows()
            {
                Action action = () =>
                {
                    new AzureServiceBusEndpointProvider<TestMessage, TestMessage, TestCommand, TestEvent, TestRequest, TestResponse>("TestEndpoint", Mock.Of<IMessagingFactory>(), Mock.Of<IMessageSerializer>(), Mock.Of<IMessageDeserializerFactory>(), null, _testAssemblyFilter, _testMessageTypeFilter, new MessagePropertyProviderManager<TestMessage>(), _mockMessageOutgoingPropertiesTable.Object);
                };

                action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("messageTypePathMappings");
            }

            [Fact]
            public void CreatingWithEmptyMessageTypePathMappingsThrows()
            {
                Action action = () =>
                {
                    new AzureServiceBusEndpointProvider<TestMessage, TestMessage, TestCommand, TestEvent, TestRequest, TestResponse>("TestEndpoint", Mock.Of<IMessagingFactory>(), Mock.Of<IMessageSerializer>(), Mock.Of<IMessageDeserializerFactory>(), new List<MessageTypeMessagingEntityMappingDetails>(), _testAssemblyFilter, _testMessageTypeFilter, new MessagePropertyProviderManager<TestMessage>(), _mockMessageOutgoingPropertiesTable.Object);
                };

                action.ShouldThrow<ArgumentException>().And.ParamName.Should().Be("messageTypePathMappings");
            }

            [Fact]
            public void CreatingWithNullMessagePropertyProviderManagerThrows()
            {
                Action action = () =>
                {
                    new AzureServiceBusEndpointProvider<TestMessage, TestMessage, TestCommand, TestEvent, TestRequest, TestResponse>("TestEndpoint", Mock.Of<IMessagingFactory>(), Mock.Of<IMessageSerializer>(), Mock.Of<IMessageDeserializerFactory>(), _messageTypePathMappings, _testAssemblyFilter, _testMessageTypeFilter, null, _mockMessageOutgoingPropertiesTable.Object);
                };

                action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("messagePropertyProviderManager");
            }

            public void CreatingWithNullMessageOutgoingMessagePropertiesTableThrows()
            {
                Action action = () =>
                {
                    new AzureServiceBusEndpointProvider<TestMessage, TestMessage, TestCommand, TestEvent, TestRequest, TestResponse>("TestEndpoint", Mock.Of<IMessagingFactory>(), Mock.Of<IMessageSerializer>(), Mock.Of<IMessageDeserializerFactory>(), _messageTypePathMappings, _testAssemblyFilter, _testMessageTypeFilter, new MessagePropertyProviderManager<TestMessage>(), null);
                };

                action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("messagePropertyProviderManager");
            }
        }

        public class CreateEndpointFacts : EndpointProviderFacts
        {
            private readonly Mock<IMessagingFactory> _mockMessagingFactory = new Mock<IMessagingFactory>();
            private readonly Mock<IMessageSerializer> _mockMessageSerializer = new Mock<IMessageSerializer>();
            private readonly Mock<IMessageDeserializerFactory> _mockMessageDeserializerFactory = new Mock<IMessageDeserializerFactory>();
            private readonly MessagePropertyProviderManager<TestMessage> _messagePropertyProviderManager = new MessagePropertyProviderManager<TestMessage>();

            public CreateEndpointFacts()
            {
            }

            [Fact]
            public void CreateEndpoint()
            {
                new AzureServiceBusEndpointProvider<TestMessage, TestMessage, TestCommand, TestEvent, TestRequest, TestResponse>(
                    "TestEndpoint", _mockMessagingFactory.Object, _mockMessageSerializer.Object, _mockMessageDeserializerFactory.Object, _messageTypePathMappings, _testAssemblyFilter, _testMessageTypeFilter, _messagePropertyProviderManager, _mockMessageOutgoingPropertiesTable.Object)
                    .CreateEndpoint();
            }

            [Fact]
            public async Task CanPublishEndpointEvents()
            {
                _messageTypePathMappings.Add(new MessageTypeMessagingEntityMappingDetails(typeof(TestSpecificEvent1), "events/subscriptions/specific-events-1", MessagingEntityType.Subscription));

                var mockMessageSender = new Mock<IMessageSender>();
                mockMessageSender.Setup(mr => mr.SendAsync(It.IsNotNull<BrokeredMessage>()))
                    .Returns(Task.FromResult(true));

                _mockMessagingFactory.Setup(mf => mf.CreateMessageSender(It.IsNotNull<Type>(), It.IsNotNull<string>()))
                    .Returns(mockMessageSender.Object);

                var endpointProvider = new AzureServiceBusEndpointProvider<TestMessage, TestMessage, TestCommand, TestEvent, TestRequest, TestResponse>(
                    "TestEndpoint", _mockMessagingFactory.Object, new JsonMessageSerializer(), new JsonMessageDeserializerFactory(typeof(JsonMessageDeserializer<>)), _messageTypePathMappings, _testAssemblyFilter, _testMessageTypeFilter, _messagePropertyProviderManager, _mockMessageOutgoingPropertiesTable.Object);

                var testEvent = new TestSpecificEvent1
                {
                    TestId = 1234
                };

                await endpointProvider.CreateEndpoint().PublishAsync(testEvent);

                _mockMessagingFactory.Verify(mf => mf.CreateMessageSender(It.Is<Type>(t => t == typeof(TestSpecificEvent1)), It.Is<string>(s => s == "events")), Times.Once());

                mockMessageSender.Verify(mr => mr.SendAsync(It.IsAny<BrokeredMessage>()), Times.Once());
            }
        }

        public class CreateEndpointClientFacts : EndpointProviderFacts
        {
            private readonly Mock<IMessagingFactory> _mockMessagingFactory = new Mock<IMessagingFactory>();
            private readonly Mock<IMessageSerializer> _mockMessageSerializer = new Mock<IMessageSerializer>();
            private readonly Mock<IMessageDeserializerFactory> _mockMessageDeserializerFactory = new Mock<IMessageDeserializerFactory>();
            private readonly MessagePropertyProviderManager<TestMessage> _messagePropertyProviderManager = new MessagePropertyProviderManager<TestMessage>();

            public CreateEndpointClientFacts()
            {
            }


            [Fact]
            public void CreateEndpointClient()
            {
                new AzureServiceBusEndpointProvider<TestMessage, TestMessage, TestCommand, TestEvent, TestRequest, TestResponse>(
                    "TestEndpoint", _mockMessagingFactory.Object, _mockMessageSerializer.Object, _mockMessageDeserializerFactory.Object, _messageTypePathMappings, _testAssemblyFilter, _testMessageTypeFilter, _messagePropertyProviderManager, _mockMessageOutgoingPropertiesTable.Object)
                    .CreateEndpointClient();
            }

            [Fact]
            public async Task CanReceiveEndpointClientEvents()
            {
                var testSubscriptionPath = "events/subscriptions/specific-events-1";

                _messageTypePathMappings.Add(new MessageTypeMessagingEntityMappingDetails(typeof(TestSpecificEvent1), testSubscriptionPath, MessagingEntityType.Subscription));

                var publishedEventBrokeredMessageTaskCompletionSource = new TaskCompletionSource<BrokeredMessage>();

                var mockMessageReceiver = new Mock<IMessageReceiver>();
                mockMessageReceiver.Setup(mr => mr.ReceiveAsync())
                    .Returns(() => publishedEventBrokeredMessageTaskCompletionSource.Task);

                _mockMessagingFactory.Setup(mf => mf.CreateMessageReceiver(It.IsNotNull<Type>(), It.IsNotNull<string>(), It.IsAny<MessageReceiveMode>()))
                    .Returns(mockMessageReceiver.Object);

                var mockMessageSender = new Mock<IMessageSender>();
                mockMessageSender.Setup(mr => mr.SendAsync(It.IsNotNull<BrokeredMessage>()))
                    .Callback<BrokeredMessage>(bm =>
                    {

                        publishedEventBrokeredMessageTaskCompletionSource.SetResult(bm);
                        publishedEventBrokeredMessageTaskCompletionSource = new TaskCompletionSource<BrokeredMessage>();
                    })
                    .Returns(Task.FromResult(true));

                _mockMessagingFactory.Setup(mf => mf.CreateMessageSender(It.IsNotNull<Type>(), It.IsNotNull<string>()))
                    .Returns(mockMessageSender.Object);

                var endpointProvider = new AzureServiceBusEndpointProvider<TestMessage, TestMessage, TestCommand, TestEvent, TestRequest, TestResponse>(
                    "TestEndpoint", _mockMessagingFactory.Object, new JsonMessageSerializer(), new JsonMessageDeserializerFactory(typeof(JsonMessageDeserializer<>)), _messageTypePathMappings, _testAssemblyFilter, _testMessageTypeFilter, _messagePropertyProviderManager, _mockMessageOutgoingPropertiesTable.Object);

                var endpointClient = endpointProvider.CreateEndpointClient();

                var publishedEvents = endpointClient.Events.Replay();

                using(publishedEvents.Connect())
                {
                    await endpointProvider.CreateEndpoint().PublishAsync(new TestSpecificEvent1
                    {
                        TestId = 1234
                    });

                    var testEvent = await publishedEvents.FirstOrDefaultAsync();

                    testEvent.Should().NotBeNull();
                    testEvent.TestId.Should().Be(1234);
                }

                _mockMessagingFactory.Verify(mf => mf.CreateMessageReceiver(It.Is<Type>(it => it == typeof(TestSpecificEvent1)), testSubscriptionPath, It.Is<MessageReceiveMode>(mrm => mrm == MessageReceiveMode.ReceiveAndDelete)), Times.Once());

                /* NOTE: it's possible ReceiveAsync will be called up to two times due to concurrency here:
                 * the first time will return the msg for the test, but then it's possible there will be a second call to ReceiveAsync to wait for the next message
                 * before the subscription is shut down.
                 */
                mockMessageReceiver.Verify(mr => mr.ReceiveAsync(), Times.AtMost(2));
            }
        }

        public class TestMessage : IMessage
        {
            public int TestId
            {
                get;
                set;
            }
        }

        public class TestCommand : TestMessage, ICommand
        {

        }

        public class TestEvent : TestMessage, IEvent
        {

        }

        public class TestSpecificEvent1 : TestEvent
        {

        }

        public class TestRequest : TestMessage, IRequest
        {
            public string RequesterId
            {
                get;
                set;
            }

            public string RequestId
            {
                get;
                set;
            }
        }

        public class TestResponse : TestMessage, IResponse
        {
            public string RequesterId
            {
                get;
                set;
            }

            public string RequestId
            {
                get;
                set;
            }
        }
    }
}
