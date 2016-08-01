using System;
using FluentAssertions;
using Moq;
using Obvs.AzureServiceBus.Infrastructure;
using Xunit;

namespace Obvs.AzureServiceBus.Tests
{
    public class PeekLockMessageControlFacts
    {
        private Mock<IMessagePeekLockControlProvider> _mockMessagePeekLockControlProvider;
        private Mock<IMessagePeekLockControl> _mockMessagePeekLockControl;

        public PeekLockMessageControlFacts()
        {
            _mockMessagePeekLockControl = new Mock<IMessagePeekLockControl>();

            _mockMessagePeekLockControlProvider = new Mock<IMessagePeekLockControlProvider>();
            _mockMessagePeekLockControlProvider.Setup(mplcp => mplcp.GetMessagePeekLockControl<TestMessage>(It.IsAny<TestMessage>()))
                .Returns(_mockMessagePeekLockControl.Object);

            MessagePeekLockControlProvider.Use(_mockMessagePeekLockControlProvider.Object);
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
            public void GetPeekLockControlForMessageInvokesProvider()
            {
                TestMessage testMessage = new TestMessage();

                testMessage.GetPeekLockControl();

                _mockMessagePeekLockControlProvider.Verify(mplcp => mplcp.GetMessagePeekLockControl(testMessage), Times.Once());
            }

            [Fact]
            public void GetPeekLockControlForMessageReturnsExpectedInstance()
            {
                new TestMessage().GetPeekLockControl().Should().Be(_mockMessagePeekLockControl.Object);
            }
        }

        internal class TestMessage
        {
        }
    }
}
