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

            IMessagePeekLockControl result = configuredMessagePeekLockControlProvider.GetMessagePeekLockControl(message);

            if(result == null)
            {
                throw new InvalidOperationException("The specified message is not being tracked for peek lock control.");
            }

            return result;
        }
    }
}
