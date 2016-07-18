using System;
using Obvs.AzureServiceBus.Infrastructure;

namespace Obvs.AzureServiceBus
{
    public static class PeekLockControlMessageExtensions
    {
        public static IMessagePeekLockControl GetPeekLockControl<TMessage>(this TMessage message) where TMessage : class
        {
            if(message == null) throw new ArgumentNullException(nameof(message));

            IMessagePeekLockControlProvider configuredMessagePeekLockControlProvider = MessagePeekLockControlProvider.Default;

            if(configuredMessagePeekLockControlProvider == null)
            {
                throw new InvalidOperationException($"No {nameof(IMessagePeekLockControlProvider)} has been configured.");
            }

            return configuredMessagePeekLockControlProvider.GetMessagePeekLockControl(message);
        }
    }
}
