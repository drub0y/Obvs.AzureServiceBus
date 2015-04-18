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
            public void CreatingWithNullMessageSenderThrows()
            {
                Action action = () =>
                {
                    new MessagePublisher<ITestMessage>((MessageSender)null, Mock.Of<IMessageSerializer>(), Mock.Of<IMessagePropertyProvider<ITestMessage>>(), Mock.Of<IBrokeredMessageRequestResponseCorrelationMapper>());
                };

                action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("messageSender");
            }

            [Fact]
            public void CreatingWithNullIMessageSenderThrows()
            {
                Action action = () =>
                {
                    new MessagePublisher<ITestMessage>((IMessageSender)null, Mock.Of<IMessageSerializer>(), Mock.Of<IMessagePropertyProvider<ITestMessage>>(), Mock.Of<IBrokeredMessageRequestResponseCorrelationMapper>());
                };

                action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("messageSender");
            }

            [Fact]
            public void CreatingWithNullSerializerThrows()
            {
                Action action = () =>
                {
                    new MessagePublisher<ITestMessage>(Mock.Of<IMessageSender>(), null, Mock.Of<IMessagePropertyProvider<ITestMessage>>(), Mock.Of<IBrokeredMessageRequestResponseCorrelationMapper>());
                };

                action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("serializer");
            }

            [Fact]
            public void CreatingWithNullPropertyProviderThrows()
            {
                Action action = () =>
                {
                    new MessagePublisher<ITestMessage>(Mock.Of<IMessageSender>(), Mock.Of<IMessageSerializer>(), null, Mock.Of<IBrokeredMessageRequestResponseCorrelationMapper>());
                };

                action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("propertyProvider");
            }

            [Fact]
            public void CreatingWithNullRequestResponseCorrelationProviderThrows()
            {
                Action action = () =>
                {
                    new MessagePublisher<ITestMessage>(Mock.Of<IMessageSender>(), Mock.Of<IMessageSerializer>(), Mock.Of<IMessagePropertyProvider<ITestMessage>>(), null);
                };

                action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("requestCorrelationProvider");
            }
        }

        public class MessagePublishingFacts
        {
            [Fact]
            public void SerializesMessage()
            {
                Mock<IMessageSerializer> mockMessageSerializer = new Mock<IMessageSerializer>();

                MessagePublisher<ITestMessage> messagePublisher = new MessagePublisher<ITestMessage>(Mock.Of<IMessageSender>(), mockMessageSerializer.Object, Mock.Of<IMessagePropertyProvider<ITestMessage>>(), Mock.Of<IBrokeredMessageRequestResponseCorrelationMapper>());

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

                Mock<IMessagePropertyProvider<ITestMessage>> mockMessagePropertyProvider = new Mock<IMessagePropertyProvider<ITestMessage>>();

                KeyValuePair<string, object>[] properties = new[]
                {
                    new KeyValuePair<string, object>("Prop1", 1),
                    new KeyValuePair<string, object>("Prop2", "two"),
                };

                mockMessagePropertyProvider.Setup(mpp => mpp.GetProperties(It.IsAny<ITestMessage>()))
                    .Returns(properties);

                MessagePublisher<ITestMessage> messagePublisher = new MessagePublisher<ITestMessage>(mockMessageSender.Object, Mock.Of<IMessageSerializer>(), mockMessagePropertyProvider.Object, Mock.Of<IBrokeredMessageRequestResponseCorrelationMapper>());

                TestMessage message = new TestMessage();

                await messagePublisher.PublishAsync(message);

                mockMessagePropertyProvider.Verify(mpp => mpp.GetProperties(It.Is<ITestMessage>(it => Object.ReferenceEquals(it, message))), Times.Once);

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

                MessagePublisher<ITestMessage> messagePublisher = new MessagePublisher<ITestMessage>(mockMessageSender.Object, Mock.Of<IMessageSerializer>(), Mock.Of<IMessagePropertyProvider<ITestMessage>>(), Mock.Of<IBrokeredMessageRequestResponseCorrelationMapper>());

                TestMessage message = new TestMessage();

                await messagePublisher.PublishAsync(message);

                brokeredMessageSent.Should().NotBeNull();

                brokeredMessageSent.Properties.Should().Contain(new KeyValuePair<string,object>(MessagePropertyNames.TypeName, typeof(TestMessage).Name), "Should have applied the mesage type name property to the BrokeredMessage.");
            }

            [Fact]
            public async Task SendsMessage()
            {
                Mock<IMessageSender> mockMessageSender = new Mock<IMessageSender>();

                MessagePublisher<ITestMessage> messagePublisher = new MessagePublisher<ITestMessage>(mockMessageSender.Object, Mock.Of<IMessageSerializer>(), Mock.Of<IMessagePropertyProvider<ITestMessage>>(), Mock.Of<IBrokeredMessageRequestResponseCorrelationMapper>());

                TestMessage message = new TestMessage();

                await messagePublisher.PublishAsync(message);

                mockMessageSender.Verify(ms => ms.SendAsync(It.IsAny<BrokeredMessage>()), Times.Once());
            }
        }

        public class RequestResponseFacts
        {
            [Fact]
            public async Task SendingRequestInvokesCorrelationProvider()
            {
                Mock<IMessageSender> mockMessageSender = new Mock<IMessageSender>();

                BrokeredMessage brokeredMessageSent = null;

                mockMessageSender.Setup(ms => ms.SendAsync(It.IsAny<BrokeredMessage>()))
                    .Callback<BrokeredMessage>(bm =>
                    {
                        brokeredMessageSent = bm;
                    })
                    .Returns(Task.FromResult<object>(null));

                Mock<IBrokeredMessageRequestResponseCorrelationMapper> mockRequestCorrelationProvider = new Mock<IBrokeredMessageRequestResponseCorrelationMapper>();

                MessagePublisher<ITestMessage> messagePublisher = new MessagePublisher<ITestMessage>(mockMessageSender.Object, Mock.Of<IMessageSerializer>(), Mock.Of<IMessagePropertyProvider<ITestMessage>>(), mockRequestCorrelationProvider.Object);

                TestRequest request = new TestRequest();

                await messagePublisher.PublishAsync(request);

                mockRequestCorrelationProvider.Verify(rcp => rcp.MapFromRequest(request, It.IsAny<BrokeredMessage>()), Times.Once());
            }
        }

        public class DefaultRequestCorrelationProviderFacts
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

                MessagePublisher<ITestMessage> messagePublisher = new MessagePublisher<ITestMessage>(mockMessageSender.Object, Mock.Of<IMessageSerializer>(), Mock.Of<IMessagePropertyProvider<ITestMessage>>(), new DefaultBrokeredMessageRequestResponseCorrelationMapper());

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

                MessagePublisher<ITestMessage> messagePublisher = new MessagePublisher<ITestMessage>(mockMessageSender.Object, Mock.Of<IMessageSerializer>(), Mock.Of<IMessagePropertyProvider<ITestMessage>>(), new DefaultBrokeredMessageRequestResponseCorrelationMapper());

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

                MessagePublisher<ITestMessage> messagePublisher = new MessagePublisher<ITestMessage>(mockMessageSender.Object, Mock.Of<IMessageSerializer>(), Mock.Of<IMessagePropertyProvider<ITestMessage>>(), new DefaultBrokeredMessageRequestResponseCorrelationMapper());

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

        public interface ITestMessage : IMessage
        {
        }

        public class TestMessage : ITestMessage
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
