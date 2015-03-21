using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Obvs.Types;
using Moq;
using Microsoft.ServiceBus.Messaging;

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
                    new MessageSource<TestMessage>(null, new [] { new Mock<IMessageDeserializer<TestMessage>>().Object });
                };

                action.ShouldThrow<ArgumentNullException>();
            }

            [Fact]
            public void CreatingWithNullDeserializersThrows()
            {
                Action action = () =>
                {
                    new MessageSource<TestMessage>(new Mock<MessageReceiver>().Object, null);
                };

                action.ShouldThrow<ArgumentNullException>();
            }
        }

        public class TestMessage : IMessage
        {
            
        }
    }
}
