using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace Obvs.AzureServiceBus.Infrastructure
{
    public interface IMessagePeekLockControl
    {
        Task AbandonAsync();
        Task CompleteAsync();
        Task RejectAsync(string reasonCode, string description);
        Task RenewLockAsync();
    }

    internal interface IBrokeredMessagePeekLockControlProvider
    {
        IMessagePeekLockControl ProvidePeekLockControl(BrokeredMessage brokeredMessage);
    }

    internal class BrokeredMessagePeekLockControlProvider : IBrokeredMessagePeekLockControlProvider
    {
        public static readonly BrokeredMessagePeekLockControlProvider Default = new BrokeredMessagePeekLockControlProvider();

        public IMessagePeekLockControl ProvidePeekLockControl(BrokeredMessage brokeredMessage)
        {
            return new BrokeredMessagePeekLockControl(brokeredMessage);
        }
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

        public Task RejectAsync(string reasonCode, string description)
        {
            return _brokeredMessage.DeadLetterAsync(reasonCode, description);
        }

        public Task RenewLockAsync()
        {
            return _brokeredMessage.RenewLockAsync();
        }
    }
}
