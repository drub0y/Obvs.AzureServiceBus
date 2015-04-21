using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
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
    public class MessageSourceFacts
    {
        public class ConstructorFacts
        {
            [Fact]
            public void CreatingWithNullMessageReceiverThrows()
            {
                Action action = () =>
                {
                    new MessageSource<TestMessage>((IMessageReceiver)null, new[] { new Mock<IMessageDeserializer<TestMessage>>().Object });
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
            public async Task ReceivesAndDeserializesSingleMessage()
            {
                Mock<IMessageDeserializer<TestMessage>> mockTestMessageDeserializer = new Mock<IMessageDeserializer<TestMessage>>();
                mockTestMessageDeserializer.Setup(md => md.GetTypeName())
                    .Returns(typeof(TestMessage).Name);

                TestMessage testMessage = new TestMessage();

                mockTestMessageDeserializer.Setup(md => md.Deserialize(It.IsAny<Stream>()))
                    .Returns(testMessage);

                BrokeredMessage testBrokeredMessage = new BrokeredMessage()
                {
                    Properties =
                    {
                        { MessagePropertyNames.TypeName, typeof(TestMessage).Name }
                    }
                };

                IObservable<BrokeredMessage> brokeredMessages = Observable.Create<BrokeredMessage>(o =>
                    {
                        o.OnNext(testBrokeredMessage);

                        o.OnCompleted();

                        return Disposable.Empty;
                    });

                MessageSource<TestMessage> messageSource = new MessageSource<TestMessage>(brokeredMessages, new[] { mockTestMessageDeserializer.Object });

                TestMessage message = await messageSource.Messages.SingleOrDefaultAsync();

                message.ShouldBeEquivalentTo(testMessage);

                // NOTE: Would be great to be able to verify that testBrokeredMessage.CompleteAsync() was called here, but I would have to build abstraction around BrokeredMessage for that because it can't be mocked (since it's sealed)

                mockTestMessageDeserializer.Verify(md => md.Deserialize(It.IsAny<Stream>()), Times.Once());
            }

            [Fact]
            public async Task ReceivesAndDeserializesMultipleMessagesInCorrectOrder()
            {
                Mock<IMessageDeserializer<TestMessage>> mockTestMessageDeserializer = new Mock<IMessageDeserializer<TestMessage>>();
                mockTestMessageDeserializer.Setup(md => md.GetTypeName())
                    .Returns(typeof(TestMessage).Name);

                const int NumberOfMessagesToGenerate = 5;
                int messageCounter = 0;

                mockTestMessageDeserializer.Setup(md => md.Deserialize(It.IsAny<Stream>()))
                    .Returns(() => new TestMessage
                    {
                        TestId = messageCounter++
                    });

                IObservable<BrokeredMessage> brokeredMessages = Observable.Create<BrokeredMessage>(o =>
                {
                    for(int messageIndex = 0; messageIndex < NumberOfMessagesToGenerate; messageIndex++)
                    {
                        o.OnNext(new BrokeredMessage
                        {
                            Properties =
                            {
                                { MessagePropertyNames.TypeName, typeof(TestMessage).Name }
                            }
                        });
                    }

                    o.OnCompleted();

                    return Disposable.Empty;
                        });

                MessageSource<TestMessage> messageSource = new MessageSource<TestMessage>(brokeredMessages, new[] { mockTestMessageDeserializer.Object });

                IList<TestMessage> messages = await messageSource.Messages.ToList();

                messages.Count.Should().Be(NumberOfMessagesToGenerate);

                for(int messageIndex = 0; messageIndex < NumberOfMessagesToGenerate; messageIndex++)
                {
                    messages[messageIndex].TestId.Should().Be(messageIndex);
                }
            }

            [Fact]
            public async Task OnlyDeliversMessagesOfTheCorrectType()
            {
                Mock<IMessageDeserializer<TestMessage>> mockTestMessageDeserializer = new Mock<IMessageDeserializer<TestMessage>>();
                mockTestMessageDeserializer.Setup(md => md.GetTypeName())
                    .Returns(typeof(TestMessage).Name);

                TestMessage testMessage = new TestMessage();

                mockTestMessageDeserializer.Setup(md => md.Deserialize(It.IsAny<Stream>()))
                    .Returns(testMessage);

                BrokeredMessage brokeredMessageThatShouldBeIgnored = new BrokeredMessage()
                {
                    Properties =
                        {
                            { MessagePropertyNames.TypeName, "SomeMessageTypeThatIDontWant" }
                        }
                };

                BrokeredMessage brokeredMessageThatShouldBeReceived = new BrokeredMessage()
                {
                    Properties =
                    {
                        { MessagePropertyNames.TypeName, typeof(TestMessage).Name }
                    }
                };

                IObservable<BrokeredMessage> brokeredMessages = Observable.Create<BrokeredMessage>(o =>
                {
                    o.OnNext(brokeredMessageThatShouldBeIgnored);

                    o.OnNext(brokeredMessageThatShouldBeReceived);

                        o.OnCompleted();

                        return Disposable.Empty;
                    });

                MessageSource<TestMessage> messageSource = new MessageSource<TestMessage>(brokeredMessages, new[] { mockTestMessageDeserializer.Object });

                TestMessage message = await messageSource.Messages.SingleOrDefaultAsync();

                message.Should().NotBeNull();

                // NOTE: Would be great to be able to verify that testBrokeredMessage.CompleteAsync() wasn't called here, but I would have to build abstraction around BrokeredMessage for that because it can't be mocked (since it's sealed)

                mockTestMessageDeserializer.Verify(md => md.Deserialize(It.IsAny<Stream>()), Times.Once());
            }
        }

        public class TransactionalMessageFacts
        {
            [Fact]
            public async Task CompletingTransactionalMessageCompletesTheAssociatedBrokeredMessage()
            {
                Mock<IMessageDeserializer<TestTransactionalMessage>> mockTestTransactionalMessageDeserializer = new Mock<IMessageDeserializer<TestTransactionalMessage>>();
                mockTestTransactionalMessageDeserializer.Setup(md => md.GetTypeName())
                    .Returns(typeof(TestMessage).Name);

                TestTransactionalMessage testTransactionalMessage = new TestTransactionalMessage();

                mockTestTransactionalMessageDeserializer.Setup(md => md.Deserialize(It.IsAny<Stream>()))
                    .Returns(testTransactionalMessage);

                BrokeredMessage brokeredMessageThatShouldBeIgnored = new BrokeredMessage()
                {
                    Properties =
                        {
                            { MessagePropertyNames.TypeName, "SomeMessageTypeThatIDontWant" }
                        }
                };

                BrokeredMessage brokeredMessageThatShouldBeReceived = new BrokeredMessage()
                {
                    Properties =
                    {
                        { MessagePropertyNames.TypeName, typeof(TestMessage).Name }
                    }
                };

                IObservable<BrokeredMessage> brokeredMessages = Observable.Create<BrokeredMessage>(o =>
                {
                    o.OnNext(brokeredMessageThatShouldBeIgnored);

                    o.OnNext(brokeredMessageThatShouldBeReceived);

                    o.OnCompleted();

                    return Disposable.Empty;
                });

                MessageSource<TestTransactionalMessage> messageSource = new MessageSource<TestTransactionalMessage>(brokeredMessages, new[] { mockTestTransactionalMessageDeserializer.Object });

                TestTransactionalMessage message = await messageSource.Messages.SingleOrDefaultAsync();

                message.Should().NotBeNull();
                message.BrokeredMessage.Should().BeSameAs(brokeredMessageThatShouldBeReceived);

                await message.CompleteAsync();
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

        public class TestTransactionalMessage : TransactionalMessage
        {
            public int TestId
            {
                get;
                set;
            }
        }
    }
}
