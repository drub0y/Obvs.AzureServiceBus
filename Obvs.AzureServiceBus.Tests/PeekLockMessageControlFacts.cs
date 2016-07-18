using System;
using FluentAssertions;
using Moq;
using Obvs.AzureServiceBus.Infrastructure;
using Obvs.Types;
using Xunit;

namespace Obvs.AzureServiceBus.Tests
{
    public class PeekLockMessageControlFacts
    {
        public PeekLockMessageControlFacts()
        {
            Mock<IMessagePeekLockControlProvider> mockMessagePeekLockControlProvider = new Mock<IMessagePeekLockControlProvider>();
            mockMessagePeekLockControlProvider.Setup(mplcp => mplcp.GetMessagePeekLockControl<TestMessage>(It.IsAny<TestMessage>()))
                .Returns(Mock.Of<IMessagePeekLockControl>());

            MessagePeekLockControlProvider.Default = mockMessagePeekLockControlProvider.Object;
        }

        public class GetPeekLockControl : PeekLockMessageControlFacts
        {
            [Fact]
            public void AttemptingToGetPeekLockControlForNullMessageThrows()
            {
                TestMessage message = null;

                Action action = () => message.GetPeekLockControl();

                action.ShouldThrow<ArgumentNullException>()
                    .And.ParamName.Should().Be("message");
            }

            [Fact]
            public void GetPeekLockControlForMessageReturnsNonNullInstance()
            {
                new TestMessage().GetPeekLockControl().Should().NotBeNull();
            }
        }

        internal class TestMessage
        {
        }
    }
}
