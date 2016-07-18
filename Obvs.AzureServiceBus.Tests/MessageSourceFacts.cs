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
                    new MessageSource<TestMessage>((IMessagingEntityFactory)null, new[] { Mock.Of<IMessageDeserializer<TestMessage>>() }, Mock.Of<IMessageBrokeredMessageTable>());
                };

                action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("messagingEntityFactory");
            }

            [Fact]
            public void CreatingWithNullBrokeredMessageObservableThrows()
            {
                Action action = () =>
                {
                    new MessageSource<TestMessage>((IObservable<BrokeredMessage>)null, new[] { Mock.Of<IMessageDeserializer<TestMessage>>() }, Mock.Of<IMessageBrokeredMessageTable>());
                };

                action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("brokeredMessages");
            }

            [Fact]
            public void CreatingWithNullDeserializersThrows()
            {
                Action action = () =>
                {
                    new MessageSource<TestMessage>(Mock.Of<IObservable<BrokeredMessage>>(), null, Mock.Of<IMessageBrokeredMessageTable>());
                };

                action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("deserializers");
            }

            [Fact]
            public void CreatingWithNullMessageBrokeredMessageTableThrows()
            {
                Action action = () =>
                {
                    new MessageSource<TestMessage>(Mock.Of<IObservable<BrokeredMessage>>(), new[] { Mock.Of<IMessageDeserializer<TestMessage>>() }, null);
                };

                action.ShouldThrow<ArgumentNullException>().And.ParamName.Should().Be("messageBrokeredMessageTable");
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

                MessageSource<TestMessage> messageSource = new MessageSource<TestMessage>(brokeredMessages, new[] { mockTestMessageDeserializer.Object }, Mock.Of<IMessageBrokeredMessageTable>());

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

                MessageSource<TestMessage> messageSource = new MessageSource<TestMessage>(brokeredMessages, new[] { mockTestMessageDeserializer.Object }, Mock.Of<IMessageBrokeredMessageTable>());

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

                MessageSource<TestMessage> messageSource = new MessageSource<TestMessage>(brokeredMessages, new[] { mockTestMessageDeserializer.Object }, Mock.Of<IMessageBrokeredMessageTable>());

                TestMessage message = await messageSource.Messages.SingleOrDefaultAsync();

                message.Should().NotBeNull();

                // NOTE: Would be great to be able to verify that testBrokeredMessage.CompleteAsync() wasn't called here, but I would have to build abstraction around BrokeredMessage for that because it can't be mocked (since it's sealed)

                mockTestMessageDeserializer.Verify(md => md.Deserialize(It.IsAny<Stream>()), Times.Once());
            }
        }

        public class PeekLockMessageFacts
        {
            [Fact]
            public async Task CompletingPeekLockMessageCompletesTheAssociatedBrokeredMessage()
            {
                Mock<IMessageDeserializer<TestPeekLockMessage>> mockTestPeekLockMessageDeserializer = new Mock<IMessageDeserializer<TestPeekLockMessage>>();
                mockTestPeekLockMessageDeserializer.Setup(md => md.GetTypeName())
                    .Returns(typeof(TestPeekLockMessage).Name);

                TestPeekLockMessage testPeekLockMessage = new TestPeekLockMessage();

                mockTestPeekLockMessageDeserializer.Setup(md => md.Deserialize(It.IsAny<Stream>()))
                    .Returns(testPeekLockMessage);

                IObservable<BrokeredMessage> brokeredMessages = Observable.Create<BrokeredMessage>(o =>
                {
                    o.OnNext(new BrokeredMessage
                    {
                        Properties =
                        {
                            { MessagePropertyNames.TypeName, typeof(TestPeekLockMessage).Name }
                        }
                    });

                    o.OnCompleted();

                    return Disposable.Empty;
                });

                Mock<IMessagePeekLockControl> mockBrokeredMessagePeekLockControl = new Mock<IMessagePeekLockControl>();

                Mock<IMessagePeekLockControlProvider> mockPeekLockControlProvider = new Mock<IMessagePeekLockControlProvider>();

                mockPeekLockControlProvider.Setup(bmplcp => bmplcp.GetMessagePeekLockControl(testPeekLockMessage))
                    .Returns(mockBrokeredMessagePeekLockControl.Object);

                MessagePeekLockControlProvider.Default = mockPeekLockControlProvider.Object;

                MessageSource<TestPeekLockMessage> messageSource = new MessageSource<TestPeekLockMessage>(brokeredMessages, new[] { mockTestPeekLockMessageDeserializer.Object }, Mock.Of<IMessageBrokeredMessageTable>());

                TestPeekLockMessage message = await messageSource.Messages.SingleOrDefaultAsync();

                IMessagePeekLockControl messagePeekLockControl = message.GetPeekLockControl();

                messagePeekLockControl.Should().NotBeNull();

                await messagePeekLockControl.CompleteAsync();

                mockBrokeredMessagePeekLockControl.Verify(bmplc => bmplc.CompleteAsync(), Times.Once());
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

        public class TestPeekLockMessage
        {
            public int TestId
            {
                get;
                set;
            }
        }
    }
}
