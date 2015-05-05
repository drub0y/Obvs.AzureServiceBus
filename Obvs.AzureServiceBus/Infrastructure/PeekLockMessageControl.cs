using System;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Obvs.Types;

namespace Obvs.AzureServiceBus.Infrastructure
{
    public static class PeekLockControlMessageExtensions
    {
        public static IMessagePeekLockControl PeekLockControl(this IMessage message)
        {
            if(message == null) throw new ArgumentNullException("message");

            IBrokeredMessageBasedMessage brokeredMessageBasedMessage = message as IBrokeredMessageBasedMessage;

            if(brokeredMessageBasedMessage == null)
            {
                throw new InvalidOperationException("The message was not received with peek-lock semantics. Please check your messaging entity configuration to ensure you are using a peek-lock receive mode.");
            }

            return new BrokeredMessagePeekLockControl(brokeredMessageBasedMessage.BrokeredMessage);
        }
    }

    public interface IMessagePeekLockControl
    {
        Task AbandonAsync();
        Task CompleteAsync();
        Task DeadLetterAsync(string reasonCode, string description);
        Task RenewLockAsync();
    }

    internal struct BrokeredMessagePeekLockControl : IMessagePeekLockControl
    {
        private readonly BrokeredMessage _brokeredMessage;

        public BrokeredMessagePeekLockControl(BrokeredMessage brokeredMessage)
        {
            _brokeredMessage = brokeredMessage;
        }

        public BrokeredMessage BrokeredMessage
        {
            get
            {
                return _brokeredMessage;
            }
        }

        public Task AbandonAsync()
        {
            return _brokeredMessage.AbandonAsync();
        }

        public Task CompleteAsync()
        {
            return _brokeredMessage.CompleteAsync();
        }

        public Task DeadLetterAsync(string reasonCode, string description)
        {
            return _brokeredMessage.DeadLetterAsync();
        }

        public Task RenewLockAsync()
        {
            return _brokeredMessage.RenewLockAsync();
        }
    }
}
