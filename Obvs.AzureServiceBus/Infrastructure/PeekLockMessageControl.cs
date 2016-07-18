using System;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace Obvs.AzureServiceBus.Infrastructure
{
    public static class MessagePeekLockControlProvider
    {
        public static IMessagePeekLockControlProvider Default;
    }

    public interface IMessagePeekLockControl
    {
        Task AbandonAsync();
        Task CompleteAsync();
        Task RejectAsync(string reasonCode, string description);
        Task RenewLockAsync();
    }

    public interface IMessagePeekLockControlProvider
    {
        IMessagePeekLockControl GetMessagePeekLockControl<TMessage>(TMessage message);
    }

    internal sealed class DefaultBrokeredMessagePeekLockControlProvider : IMessagePeekLockControlProvider
    {
        private IMessageBrokeredMessageTable _messageBrokeredMessageTable;

        public DefaultBrokeredMessagePeekLockControlProvider(IMessageBrokeredMessageTable messageBrokeredMessageTable)
        {
            _messageBrokeredMessageTable = messageBrokeredMessageTable;
        }

        public IMessagePeekLockControl GetMessagePeekLockControl<TMessage>(TMessage message) => new DefaultBrokeredMessagePeekLockControl(message, _messageBrokeredMessageTable.GetBrokeredMessageForMessage(message), _messageBrokeredMessageTable.RemoveBrokeredMessageForMessage);
    }

    internal struct DefaultBrokeredMessagePeekLockControl : IMessagePeekLockControl
    {
        private object _message;
        private BrokeredMessage _brokeredMessage;
        private Action<object> _removeMessageTrackingCallback;

        public DefaultBrokeredMessagePeekLockControl(object message, BrokeredMessage brokeredMessage, Action<object> removeMessageTrackingCallback)
        {
            _message = message;
            _brokeredMessage = brokeredMessage;
            _removeMessageTrackingCallback = removeMessageTrackingCallback;
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
            EnsureBrokeredMessageNotAlreadyProcessed();

            return _brokeredMessage.RenewLockAsync();
        }

        private async Task PerformBrokeredMessageActionAndDisposeAsync(Func<BrokeredMessage, Task> action)
        {
            EnsureBrokeredMessageNotAlreadyProcessed();

            await action(_brokeredMessage);

            _brokeredMessage.Dispose();
            _brokeredMessage = null;

            _removeMessageTrackingCallback(_message);
            _message = null;
        }

        private void EnsureBrokeredMessageNotAlreadyProcessed()
        {
            if(_message == null)
            {
                throw new InvalidOperationException("The message has already been abandoned, completed or rejected.");
            }
        }
    }
}
