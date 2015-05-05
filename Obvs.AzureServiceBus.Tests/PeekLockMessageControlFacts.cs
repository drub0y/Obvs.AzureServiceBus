using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Obvs.Types;
using Xunit;
using FluentAssertions;
using Obvs.AzureServiceBus;
using Moq;

namespace Obvs.AzureServiceBus.Tests
{
    public class PeekLockMessageControlFacts
    {
        public class GetPeekLockControl
        {
            [Fact]
            public void AttemptingToGetPeekLockControlForNullMessageThrows()
            {
                IMessage message = null;

                Action action = () => message.GetPeekLockControl();

                action.ShouldThrow<ArgumentNullException>()
                    .And.ParamName.Should().Be("message");
            }

            [Fact]
            public void AttemptingToGetPeekLockControlForNonPeekLockBasedMessageThrows()
            {
                IMessage message = Mock.Of<IMessage>();

                Action action = () => message.GetPeekLockControl();

                action.ShouldThrow<InvalidOperationException>();
            }
        }

    }
}
