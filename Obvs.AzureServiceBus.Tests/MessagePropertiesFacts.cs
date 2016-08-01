using System;
using FluentAssertions;
using Moq;
using Obvs.AzureServiceBus.Infrastructure;
using Xunit;

namespace Obvs.AzureServiceBus.Tests
{
    public class MessagePropertiesFacts
    {
        private Mock<IMessagePropertiesProvider> _mockMessagePropertiesProvider;
        private Mock<IIncomingMessageProperties> _mockIncomingMessageProperties;
        private Mock<IOutgoingMessageProperties> _mockOutgoingMessageProperties;

        public MessagePropertiesFacts()
        {
            _mockMessagePropertiesProvider = new Mock<IMessagePropertiesProvider>();

            _mockIncomingMessageProperties = new Mock<IIncomingMessageProperties>();
            _mockOutgoingMessageProperties = new Mock<IOutgoingMessageProperties>();

            _mockMessagePropertiesProvider.Setup(mpp => mpp.GetIncomingMessageProperties(It.IsAny<object>()))
                .Returns(_mockIncomingMessageProperties.Object);

            _mockMessagePropertiesProvider.Setup(mpp => mpp.GetOutgoingMessageProperties(It.IsAny<object>()))
                .Returns(_mockOutgoingMessageProperties.Object);

            MessagePropertiesProvider.Use(_mockMessagePropertiesProvider.Object);
        }

        public class GetIncomingPropertiesFacts : MessagePropertiesFacts
        {
            [Fact]
            public void AttemptingToGetIncomingMessagePropertiesForNullMessageThrows()
            {
                TestMessage message = null;

                Action action = () => message.GetIncomingMessageProperties();

                action.ShouldThrow<ArgumentNullException>()
                    .And.ParamName.Should().Be("message");
            }

            [Fact]
            public void GetIncomingMessagePropertiesForMessageInvokesProvider()
            {
                TestMessage testMessage = new TestMessage();

                testMessage.GetIncomingMessageProperties();

                _mockMessagePropertiesProvider.Verify(mplcp => mplcp.GetIncomingMessageProperties(testMessage), Times.Once());
            }

            [Fact]
            public void GetIncomingMessagePropertiesForMessageReturnsExpectedInstance()
            {
                new TestMessage().GetIncomingMessageProperties().Should().Be(_mockIncomingMessageProperties.Object);
            }
        }

        public class GetOutgoingPropertiesFacts : MessagePropertiesFacts
        {
            [Fact]
            public void AttemptingToGetOutgoingMessagePropertiesForNullMessageThrows()
            {
                TestMessage message = null;

                Action action = () => message.GetOutgoingMessageProperties();

                action.ShouldThrow<ArgumentNullException>()
                    .And.ParamName.Should().Be("message");
            }

            [Fact]
            public void GetOutgoingMessagePropertiesForMessageInvokesProvider()
            {
                TestMessage testMessage = new TestMessage();

                testMessage.GetOutgoingMessageProperties();

                _mockMessagePropertiesProvider.Verify(mplcp => mplcp.GetOutgoingMessageProperties(testMessage), Times.Once());
            }

            [Fact]
            public void GetOutgoingMessagePropertiesForMessageReturnsExpectedInstance()
            {
                new TestMessage().GetOutgoingMessageProperties().Should().Be(_mockOutgoingMessageProperties.Object);
            }
        }

        internal class TestMessage
        {
        }
    }
}
