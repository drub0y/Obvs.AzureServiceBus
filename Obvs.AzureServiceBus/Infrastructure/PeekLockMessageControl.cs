using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace Obvs.AzureServiceBus.Infrastructure
{
    public static class MessagePeekLockControlProvider
    {
        public static IMessagePeekLockControlProvider Default;

        internal static void SetDefaultInstance(IMessagePeekLockControlProvider defaultBrokeredMessagePeekLockControlProvider)
        {
            if(defaultBrokeredMessagePeekLockControlProvider == null) throw new ArgumentNullException(nameof(defaultBrokeredMessagePeekLockControlProvider));

            MessagePeekLockControlProvider.Default = defaultBrokeredMessagePeekLockControlProvider;
        }
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

    internal interface IBrokeredMessagePeekLockControlProvider : IMessagePeekLockControlProvider
    {
        void ProvidePeekLockControl<TMessage>(TMessage message, BrokeredMessage brokeredMessage);
    }

    internal sealed class DefaultBrokeredMessagePeekLockControlProvider : IBrokeredMessagePeekLockControlProvider
    {
        private ConditionalWeakTable<object, IMessagePeekLockControl> _trackedMessagePeekLockControlTable = new ConditionalWeakTable<object, IMessagePeekLockControl>();

        public void ProvidePeekLockControl<TMessage>(TMessage message, BrokeredMessage brokeredMessage)
        {
            _trackedMessagePeekLockControlTable.Add(message, new BrokeredMessagePeekLockControl(this, message, brokeredMessage));
        }

        public IMessagePeekLockControl GetMessagePeekLockControl<TMessage>(TMessage message)
        {
            IMessagePeekLockControl result;

            _trackedMessagePeekLockControlTable.TryGetValue(message, out result);

            return result;
        }

        internal void NotifyMessageCompletion(object message)
        {
            _trackedMessagePeekLockControlTable.Remove(message);
        }
    }

    internal sealed class BrokeredMessagePeekLockControl : IMessagePeekLockControl
    {
        private DefaultBrokeredMessagePeekLockControlProvider _provider;
        private object _message;
        private BrokeredMessage _brokeredMessage;

        public BrokeredMessagePeekLockControl(DefaultBrokeredMessagePeekLockControlProvider provider, object message, BrokeredMessage brokeredMessage)
        {
            _provider = provider;
            _message = message;
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
            EnsureBrokeredMessageNotAlreadyProcessed();

            return _brokeredMessage.RenewLockAsync();
        }

        private async Task PerformBrokeredMessageActionAndDisposeAsync(Func<BrokeredMessage, Task> action)
        {
            EnsureBrokeredMessageNotAlreadyProcessed();

            await action(_brokeredMessage);

            _provider.NotifyMessageCompletion(_message);

            _brokeredMessage.Dispose();
            _brokeredMessage = null;
        }

        private void EnsureBrokeredMessageNotAlreadyProcessed()
        {
            if(_message == null)
            {
                throw new InvalidOperationException("The brokered message has already been abandoned, completed or rejected.");
            }
        }
    }
}
