using System;
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
        private BrokeredMessage _brokeredMessage;

        public BrokeredMessagePeekLockControl(BrokeredMessage brokeredMessage)
        {
            _brokeredMessage = brokeredMessage;
        }

        public async Task AbandonAsync()
        {
            await PerformBrokeredMessageActionAndDisposeAsync(async bm => await bm.AbandonAsync());
        }

        public async Task CompleteAsync()
        {
            await PerformBrokeredMessageActionAndDisposeAsync(async bm => await bm.CompleteAsync());
        }

        public async Task RejectAsync(string reasonCode, string description)
        {
            await PerformBrokeredMessageActionAndDisposeAsync(async bm => await bm.DeadLetterAsync(reasonCode, description));
        }

        public Task RenewLockAsync()
        {
            this.EnsureBrokeredMessageNotAlreadyProcessed();

            return _brokeredMessage.RenewLockAsync();
        }

        private async Task PerformBrokeredMessageActionAndDisposeAsync(Func<BrokeredMessage, Task> action)
        {
            this.EnsureBrokeredMessageNotAlreadyProcessed();

            await action(_brokeredMessage);

            _brokeredMessage.Dispose();
            _brokeredMessage = null;
        }

        private void EnsureBrokeredMessageNotAlreadyProcessed()
        {
            if(_brokeredMessage == null)
            {
                throw new InvalidOperationException("The brokered message has already been abandoned, completed or rejected.");
            }
        }
    }
}
