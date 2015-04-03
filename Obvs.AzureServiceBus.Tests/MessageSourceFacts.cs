using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.ServiceBus.Messaging;
using Moq;
using Obvs.MessageProperties;
using Obvs.Serialization;
using Obvs.Types;
using Xunit;

namespace Obvs.AzureServiceBus.Tests
{
    public class MessageSourceFacts
    {
        public class ConstructorFacts
        {
            [Fact]
            public void CreatingWithNullMessageReceiverThrows()
            {
                Action action = () =>
                {
                    new MessageSource<TestMessage>((MessageReceiver)null, new[] { new Mock<IMessageDeserializer<TestMessage>>().Object });
                };

                action.ShouldThrow<ArgumentNullException>().Where(e => e.ParamName == "messageReceiver");
            }

            [Fact]
            public void CreatingWithNullBrokeredMessageObservableThrows()
            {
                Action action = () =>
                {
                    new MessageSource<TestMessage>((IObservable<BrokeredMessage>)null, new[] { new Mock<IMessageDeserializer<TestMessage>>().Object });
                };

                action.ShouldThrow<ArgumentNullException>().Where(e => e.ParamName == "brokeredMessages");
            }

            [Fact]
            public void CreatingWithNullDeserializersThrows()
            {
                Action action = () =>
                {
                    new MessageSource<TestMessage>(new Mock<IObservable<BrokeredMessage>>().Object, null);
                };

                action.ShouldThrow<ArgumentNullException>().Where(e => e.ParamName == "deserializers");
            }
        }

        public class MessageProcessingFacts
        {

            [Fact]
            public async Task ReceivesAndDeserializesMessage()
            {
                Mock<IMessageDeserializer<TestMessage>> mockTestMessageDeserializer = new Mock<IMessageDeserializer<TestMessage>>();
                mockTestMessageDeserializer.Setup(md => md.GetTypeName())
                    .Returns(typeof(TestMessage).Name);

                TestMessage testMessage = new TestMessage();

                mockTestMessageDeserializer.Setup(md => md.Deserialize(It.IsAny<object>()))
                    .Returns(testMessage);

                IObservable<BrokeredMessage> brokeredMessages = Observable.Create<BrokeredMessage>(o =>
                    {
                        o.OnNext(new BrokeredMessage()
                        {
                            Properties =
                            {
                                { MessagePropertyNames.TypeName, typeof(TestMessage).Name }
                            }
                        });

                        o.OnCompleted();

                        return Disposable.Empty;
                    });

                MessageSource<TestMessage> messageSource = new MessageSource<TestMessage>(brokeredMessages, new[] { mockTestMessageDeserializer.Object });

                TestMessage message = await messageSource.Messages.SingleOrDefaultAsync();

                message.ShouldBeEquivalentTo(testMessage);

                mockTestMessageDeserializer.Verify(md => md.Deserialize(It.IsAny<object>()), Times.Once());
            }
        }

        public class TestMessage : IMessage
        {

        }
    }
}
