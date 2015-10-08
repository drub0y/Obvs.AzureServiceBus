using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.ServiceBus.Messaging;
using Moq;
using Obvs.AzureServiceBus.Infrastructure;
using Obvs.MessageProperties;
using Obvs.Serialization;
using Obvs.Types;
using Xunit;

namespace Obvs.AzureServiceBus.Tests
{
    public class MessagePublisherFacts
    {
        public class ConstructorFacts
        {
            [Fact]
            public void CreatingWithNullIMessageClientEntityFactorySenderThrows()
            {
                Action action = () =>
                {
                    new MessagePublisher<TestMessage>((IMessageClientEntityFactory)null, new Mock<IMessageSerializer>().Object, new Mock<IMessagePropertyProvider<TestMessage>>().Object);
                };

                action.ShouldThrow<ArgumentNullException>().Where(e => e.ParamName == "messageClientEntityFactory");
            }

            [Fact]
            public void CreatingWithNullSerializerThrows()
            {
                Action action = () =>
                {
                    new MessagePublisher<TestMessage>(Mock.Of<IMessageClientEntityFactory>(), null, new Mock<IMessagePropertyProvider<TestMessage>>().Object);
                };

                action.ShouldThrow<ArgumentNullException>().Where(e => e.ParamName == "serializer");
            }

            [Fact]
            public void CreatingWithNullPropertyProviderThrows()
            {
                Action action = () =>
                {
                    new MessagePublisher<TestMessage>(Mock.Of<IMessageClientEntityFactory>(), new Mock<IMessageSerializer>().Object, null);
                };

                action.ShouldThrow<ArgumentNullException>().Where(e => e.ParamName == "propertyProvider");
            }
        }

        public class MessagePublishingFacts
        {
            [Fact]
            public void SerializesMessage()
            {
                Mock<IMessageSerializer> mockMessageSerializer = new Mock<IMessageSerializer>();

                MessagePublisher<TestMessage> messagePublisher = new MessagePublisher<TestMessage>(Mock.Of<IMessageClientEntityFactory>(), mockMessageSerializer.Object, Mock.Of<IMessagePropertyProvider<TestMessage>>());

                TestMessage message = new TestMessage();

                messagePublisher.PublishAsync(message);

                mockMessageSerializer.Verify(ms => ms.Serialize(It.IsAny<Stream>(), It.Is<TestMessage>(it => Object.ReferenceEquals(it, message))), Times.Once());
            }

            [Fact]
            public async Task GetsMessagePropertiesFromPropertyProviderAndAppliesThemToTheBrokeredMessage()
            {
                Mock<IMessageSender> mockMessageSender = new Mock<IMessageSender>();
                BrokeredMessage brokeredMessageSent = null;

                mockMessageSender.Setup(ms => ms.SendAsync(It.IsAny<BrokeredMessage>()))
                    .Callback<BrokeredMessage>(bm =>
                    {
                        brokeredMessageSent = bm;
                    })
                    .Returns(Task.FromResult<object>(null));

                Mock<IMessageClientEntityFactory> mockMessageClientEntityFactory = new Mock<IMessageClientEntityFactory>();
                mockMessageClientEntityFactory.Setup(mcef => mcef.CreateMessageSender(It.IsAny<Type>()))
                    .Returns(mockMessageSender.Object);

                Mock<IMessageSerializer> mockMessageSerializer = new Mock<IMessageSerializer>();

                Mock<IMessagePropertyProvider<TestMessage>> mockMessagePropertyProvider = new Mock<IMessagePropertyProvider<TestMessage>>();

                KeyValuePair<string, object>[] properties = new[]
                {
                    new KeyValuePair<string, object>("Prop1", 1),
                    new KeyValuePair<string, object>("Prop2", "two"),
                };

                mockMessagePropertyProvider.Setup(mpp => mpp.GetProperties(It.IsAny<TestMessage>()))
                    .Returns(properties);

                MessagePublisher<TestMessage> messagePublisher = new MessagePublisher<TestMessage>(mockMessageClientEntityFactory.Object, mockMessageSerializer.Object, mockMessagePropertyProvider.Object);

                TestMessage message = new TestMessage();

                await messagePublisher.PublishAsync(message);

                mockMessagePropertyProvider.Verify(mpp => mpp.GetProperties(It.Is<TestMessage>(it => Object.ReferenceEquals(it, message))), Times.Once);

                brokeredMessageSent.Should().NotBeNull();

                // TODO: should be able to do this cleaner
                brokeredMessageSent.Properties.Should().ContainKeys(properties.Select(kvp => kvp.Key), "Should have translated all properties provided by the property provider to the BrokeredMessage.");
                brokeredMessageSent.Properties.Should().ContainValues(properties.Select(kvp => kvp.Value), "Should have translated all properties provided by the property provider to the BrokeredMessage.");
            }

            [Fact]
            public async Task AppliesMessageTypeNamePropertyToTheBrokeredMessage()
            {
                Mock<IMessageSender> mockMessageSender = new Mock<IMessageSender>();
                BrokeredMessage brokeredMessageSent = null;

                mockMessageSender.Setup(ms => ms.SendAsync(It.IsAny<BrokeredMessage>()))
                    .Callback<BrokeredMessage>(bm =>
                    {
                        brokeredMessageSent = bm;
                    })
                    .Returns(Task.FromResult<object>(null));

                Mock<IMessageClientEntityFactory> mockMessageClientEntityFactory = new Mock<IMessageClientEntityFactory>();
                mockMessageClientEntityFactory.Setup(mcef => mcef.CreateMessageSender(It.IsAny<Type>()))
                    .Returns(mockMessageSender.Object);

                Mock<IMessageSerializer> mockMessageSerializer = new Mock<IMessageSerializer>();

                Mock<IMessagePropertyProvider<TestMessage>> mockMessagePropertyProvider = new Mock<IMessagePropertyProvider<TestMessage>>();

                MessagePublisher<TestMessage> messagePublisher = new MessagePublisher<TestMessage>(mockMessageClientEntityFactory.Object, mockMessageSerializer.Object, mockMessagePropertyProvider.Object);

                TestMessage message = new TestMessage();

                await messagePublisher.PublishAsync(message);

                brokeredMessageSent.Should().NotBeNull();

                brokeredMessageSent.Properties.Should().Contain(new KeyValuePair<string,object>(MessagePropertyNames.TypeName, typeof(TestMessage).Name), "Should have applied the mesage type name property to the BrokeredMessage.");
            }

            [Fact]
            public async Task SendsMessage()
            {
                Mock<IMessageSender> mockMessageSender = new Mock<IMessageSender>();
                Mock<IMessageClientEntityFactory> mockMessageClientEntityFactory = new Mock<IMessageClientEntityFactory>();
                mockMessageClientEntityFactory.Setup(mcef => mcef.CreateMessageSender(It.IsAny<Type>()))
                    .Returns(mockMessageSender.Object);

                MessagePublisher<TestMessage> messagePublisher = new MessagePublisher<TestMessage>(mockMessageClientEntityFactory.Object, Mock.Of<IMessageSerializer>(), Mock.Of<IMessagePropertyProvider<TestMessage>>());

                TestMessage message = new TestMessage();

                await messagePublisher.PublishAsync(message);

                mockMessageSender.Verify(ms => ms.SendAsync(It.IsAny<BrokeredMessage>()), Times.Once());
            }
        }

        public class RequestResponseFacts
        {
            [Fact]
            public async Task RequestWithNoRequestorIdOrRequestIdEndsUpWithNoCorrelationData()
            {
                Mock<IMessageSender> mockMessageSender = new Mock<IMessageSender>();

                BrokeredMessage brokeredMessageSent = null;

                mockMessageSender.Setup(ms => ms.SendAsync(It.IsAny<BrokeredMessage>()))
                    .Callback<BrokeredMessage>(bm =>
                    {
                        brokeredMessageSent = bm;
                    })
                    .Returns(Task.FromResult<object>(null));

                Mock<IMessageClientEntityFactory> mockMessageClientEntityFactory = new Mock<IMessageClientEntityFactory>();
                mockMessageClientEntityFactory.Setup(mcef => mcef.CreateMessageSender(It.IsAny<Type>()))
                    .Returns(mockMessageSender.Object);

                MessagePublisher<TestMessage> messagePublisher = new MessagePublisher<TestMessage>(mockMessageClientEntityFactory.Object, Mock.Of<IMessageSerializer>(), Mock.Of<IMessagePropertyProvider<TestMessage>>());

                TestRequest request = new TestRequest();

                await messagePublisher.PublishAsync(request);

                mockMessageSender.Verify(ms => ms.SendAsync(It.IsAny<BrokeredMessage>()), Times.Once());

                brokeredMessageSent.Should().NotBeNull();
                brokeredMessageSent.CorrelationId.Should().BeNull();
                brokeredMessageSent.ReplyToSessionId.Should().BeNull();
            }
            
            [Fact]
            public async Task RequestWithRequestIdEndsUpWithCorrelationId()
            {
                Mock<IMessageSender> mockMessageSender = new Mock<IMessageSender>();

                BrokeredMessage brokeredMessageSent = null;

                mockMessageSender.Setup(ms => ms.SendAsync(It.IsAny<BrokeredMessage>()))
                    .Callback<BrokeredMessage>(bm =>
                    {
                        brokeredMessageSent = bm;
                    })
                    .Returns(Task.FromResult<object>(null));

                Mock<IMessageClientEntityFactory> mockMessageClientEntityFactory = new Mock<IMessageClientEntityFactory>();
                mockMessageClientEntityFactory.Setup(mcef => mcef.CreateMessageSender(It.IsAny<Type>()))
                    .Returns(mockMessageSender.Object);

                MessagePublisher<TestMessage> messagePublisher = new MessagePublisher<TestMessage>(mockMessageClientEntityFactory.Object, Mock.Of<IMessageSerializer>(), Mock.Of<IMessagePropertyProvider<TestMessage>>());

                TestRequest request = new TestRequest
                {
                    RequestId = "TestRequestId"
                };

                await messagePublisher.PublishAsync(request);

                mockMessageSender.Verify(ms => ms.SendAsync(It.IsAny<BrokeredMessage>()), Times.Once());

                brokeredMessageSent.Should().NotBeNull();
                brokeredMessageSent.CorrelationId.Should().Be(request.RequestId);
            }

            [Fact]
            public async Task RequestRequesterIdEndsUpAsReplyToSessionId()
            {
                Mock<IMessageSender> mockMessageSender = new Mock<IMessageSender>();

                BrokeredMessage brokeredMessageSent = null;

                mockMessageSender.Setup(ms => ms.SendAsync(It.IsAny<BrokeredMessage>()))
                    .Callback<BrokeredMessage>(bm =>
                    {
                        brokeredMessageSent = bm;
                    })
                    .Returns(Task.FromResult<object>(null));

                Mock<IMessageClientEntityFactory> mockMessageClientEntityFactory = new Mock<IMessageClientEntityFactory>();
                mockMessageClientEntityFactory.Setup(mcef => mcef.CreateMessageSender(It.IsAny<Type>()))
                    .Returns(mockMessageSender.Object);

                MessagePublisher<TestMessage> messagePublisher = new MessagePublisher<TestMessage>(mockMessageClientEntityFactory.Object, Mock.Of<IMessageSerializer>(), Mock.Of<IMessagePropertyProvider<TestMessage>>());

                TestRequest request = new TestRequest
                {
                    RequesterId = "TestRequesterId"
                };

                await messagePublisher.PublishAsync(request);

                mockMessageSender.Verify(ms => ms.SendAsync(It.IsAny<BrokeredMessage>()), Times.Once());

                brokeredMessageSent.Should().NotBeNull();
                brokeredMessageSent.ReplyToSessionId.Should().Be(request.RequesterId);
            }
        }

        public class TestMessage : IMessage
        {

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
    }
}
