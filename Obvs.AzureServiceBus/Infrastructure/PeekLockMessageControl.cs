using System;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace Obvs.AzureServiceBus.Infrastructure
{
    public static class MessagePeekLockControlProvider
    {
        private static IMessagePeekLockControlProvider Instance;

        internal static IMessagePeekLockControlProvider ConfiguredInstance
        {
            get
            {
                if(Instance == null)
                {
                    UseDefault();
                }

                return Instance;
            }
        }

        public static void Use(IMessagePeekLockControlProvider messagePeekLockControlProvider)
        {
            MessagePeekLockControlProvider.Instance = messagePeekLockControlProvider;
        }

        public static void UseDefault() => Use(new DefaultBrokeredMessagePeekLockControlProvider(MessageBrokeredMessageTable.ConfiguredInstance));

        public static void UseFakeMessagePeekLockControlProvider() => Use(new FakeMessagePeekLockControlProvider());
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

        public IMessagePeekLockControl GetMessagePeekLockControl<TMessage>(TMessage message) => new DefaultBrokeredMessagePeekLockControl(_messageBrokeredMessageTable.GetBrokeredMessageForMessage(message));

        private sealed class DefaultBrokeredMessagePeekLockControl : IMessagePeekLockControl
        {
            private BrokeredMessage _brokeredMessage;

            public DefaultBrokeredMessagePeekLockControl(BrokeredMessage brokeredMessage)
            {
                _brokeredMessage = brokeredMessage;
            }

            public async Task AbandonAsync() => await PerformBrokeredMessageActionAndDisposeAsync(async bm => await bm.AbandonAsync());

            public async Task CompleteAsync() => await PerformBrokeredMessageActionAndDisposeAsync(async bm => await bm.CompleteAsync());

            public async Task RejectAsync(string reasonCode, string description) => await PerformBrokeredMessageActionAndDisposeAsync(async bm => await bm.DeadLetterAsync(reasonCode, description));

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
            }

            private void EnsureBrokeredMessageNotAlreadyProcessed()
            {
                if(_brokeredMessage == null)
                {
                    throw new InvalidOperationException("The message has already been abandoned, completed or rejected.");
                }
            }
        }
    }

    internal sealed class FakeMessagePeekLockControlProvider : IMessagePeekLockControlProvider
    {
        public IMessagePeekLockControl GetMessagePeekLockControl<TMessage>(TMessage message) => FakeMessagePeekLockControl.Default;

        private sealed class FakeMessagePeekLockControl : IMessagePeekLockControl
        {
            public static readonly FakeMessagePeekLockControl Default = new FakeMessagePeekLockControl();
            private static readonly Task CompletedPeekLockOperationTask = Task.FromResult<object>(null);

            public Task AbandonAsync() => FakeMessagePeekLockControl.CompletedPeekLockOperationTask;

            public Task CompleteAsync() => FakeMessagePeekLockControl.CompletedPeekLockOperationTask;

            public Task RejectAsync(string reasonCode, string description) => FakeMessagePeekLockControl.CompletedPeekLockOperationTask;

            public Task RenewLockAsync() => FakeMessagePeekLockControl.CompletedPeekLockOperationTask;
        }
    }

}
