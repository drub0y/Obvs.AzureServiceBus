using System;
using System.Collections.Generic;
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
                    new MessagePublisher<TestMessage>((MessageSender)null, new Mock<IMessageSerializer>().Object, new Mock<IMessagePropertyProvider<TestMessage>>().Object);
                };

                action.ShouldThrow<ArgumentNullException>().Where(e => e.ParamName == "messageSender");
            }

            [Fact]
            public void CreatingWithNullIMessageSenderThrows()
            {
                Action action = () =>
                {
                    new MessagePublisher<TestMessage>((IMessageSender)null, new Mock<IMessageSerializer>().Object, new Mock<IMessagePropertyProvider<TestMessage>>().Object);
                };

                action.ShouldThrow<ArgumentNullException>().Where(e => e.ParamName == "messageSender");
            }

            [Fact]
            public void CreatingWithNullSerializerThrows()
            {
                Action action = () =>
                {
                    new MessagePublisher<TestMessage>(new Mock<IMessageSender>().Object, null, new Mock<IMessagePropertyProvider<TestMessage>>().Object);
                };

                action.ShouldThrow<ArgumentNullException>().Where(e => e.ParamName == "serializer");
            }

            [Fact]
            public void CreatingWithNullPropertyProviderThrows()
            {
                Action action = () =>
                {
                    new MessagePublisher<TestMessage>(new Mock<IMessageSender>().Object, new Mock<IMessageSerializer>().Object, null);
                };

                action.ShouldThrow<ArgumentNullException>().Where(e => e.ParamName == "propertyProvider");
            }
        }

        public class MessagePublishingFacts
        {
            [Fact]
            public void SerializesMessage()
            {
                Mock<IMessageSender> mockMessageSender = new Mock<IMessageSender>();

                Mock<IMessageSerializer> mockMessageSerializer = new Mock<IMessageSerializer>();

                Mock<IMessagePropertyProvider<TestMessage>> mockMessagePropertyProvider = new Mock<IMessagePropertyProvider<TestMessage>>();
                
                MessagePublisher<TestMessage> messagePublisher = new MessagePublisher<TestMessage>(mockMessageSender.Object, mockMessageSerializer.Object, mockMessagePropertyProvider.Object);

                TestMessage message = new TestMessage();

                messagePublisher.PublishAsync(message);

                mockMessageSerializer.Verify(ms => ms.Serialize(It.Is<TestMessage>(it => Object.ReferenceEquals(it, message))), Times.Once());
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

                Mock<IMessageSerializer> mockMessageSerializer = new Mock<IMessageSerializer>();

                Mock<IMessagePropertyProvider<TestMessage>> mockMessagePropertyProvider = new Mock<IMessagePropertyProvider<TestMessage>>();

                KeyValuePair<string, object>[] properties = new[]
                {
                    new KeyValuePair<string, object>("Prop1", 1),
                    new KeyValuePair<string, object>("Prop2", "two"),
                };

                mockMessagePropertyProvider.Setup(mpp => mpp.GetProperties(It.IsAny<TestMessage>()))
                    .Returns(properties);

                MessagePublisher<TestMessage> messagePublisher = new MessagePublisher<TestMessage>(mockMessageSender.Object, mockMessageSerializer.Object, mockMessagePropertyProvider.Object);

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

                Mock<IMessageSerializer> mockMessageSerializer = new Mock<IMessageSerializer>();

                Mock<IMessagePropertyProvider<TestMessage>> mockMessagePropertyProvider = new Mock<IMessagePropertyProvider<TestMessage>>();

                MessagePublisher<TestMessage> messagePublisher = new MessagePublisher<TestMessage>(mockMessageSender.Object, mockMessageSerializer.Object, mockMessagePropertyProvider.Object);

                TestMessage message = new TestMessage();

                await messagePublisher.PublishAsync(message);

                brokeredMessageSent.Should().NotBeNull();

                brokeredMessageSent.Properties.Should().Contain(new KeyValuePair<string,object>(MessagePropertyNames.TypeName, typeof(TestMessage).Name), "Should have applied the mesage type name property to the BrokeredMessage.");
            }

            [Fact]
            public async Task SendsMessage()
            {
                Mock<IMessageSender> mockMessageSender = new Mock<IMessageSender>();

                Mock<IMessageSerializer> mockMessageSerializer = new Mock<IMessageSerializer>();

                Mock<IMessagePropertyProvider<TestMessage>> mockMessagePropertyProvider = new Mock<IMessagePropertyProvider<TestMessage>>();

                MessagePublisher<TestMessage> messagePublisher = new MessagePublisher<TestMessage>(mockMessageSender.Object, mockMessageSerializer.Object, mockMessagePropertyProvider.Object);

                TestMessage message = new TestMessage();

                await messagePublisher.PublishAsync(message);

                mockMessageSender.Verify(ms => ms.SendAsync(It.IsAny<BrokeredMessage>()), Times.Once());
            }
        }

        public class TestMessage : IMessage
        {

        }
    }
}
