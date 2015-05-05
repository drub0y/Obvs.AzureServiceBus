using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace Obvs.AzureServiceBus.Infrastructure
{
    internal interface IMessagePeekLockControlProvider
    {
        IMessagePeekLockControl ProvidePeekLockControl(BrokeredMessage brokeredMessage);
    }

    internal class MessagePeekLockControlProvider : IMessagePeekLockControlProvider
    {
        public static readonly MessagePeekLockControlProvider Default = new MessagePeekLockControlProvider();

        public IMessagePeekLockControl ProvidePeekLockControl(BrokeredMessage brokeredMessage)
        {
            return new BrokeredMessagePeekLockControlWrapper(brokeredMessage);
        }
    }

    internal interface IMessagePeekLockControl
    {
        Task AbandonAsync();
        Task CompleteAsync();
        Task DeadLetterAsync(string reasonCode, string description);
        Task RenewLockAsync();
    }

    internal sealed class BrokeredMessagePeekLockControlWrapper : IMessagePeekLockControl
    {
        private readonly BrokeredMessage _brokeredMessage;

        public BrokeredMessagePeekLockControlWrapper(BrokeredMessage brokeredMessage)
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
