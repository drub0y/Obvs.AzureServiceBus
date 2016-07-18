using System;
using Obvs.AzureServiceBus.Infrastructure;

namespace Obvs.AzureServiceBus
{
    public static class MessagePropertiesMessageExtensions
    {
        public static IOutgoingMessageProperties GetOutgoingMessageProperties<TMessage>(this TMessage message) where TMessage : class
        {
            if(message == null) throw new ArgumentNullException(nameof(message));

            IMessagePropertiesProvider configuredMessagePropertyProvider = MessagePropertiesProvider.Default;

            if(configuredMessagePropertyProvider == null)
            {
                throw new InvalidOperationException($"No {nameof(IMessagePropertiesProvider)} has been configured.");
            }

            return configuredMessagePropertyProvider.GetOutgoingMessageProperties(message);
        }

        public static IIncomingMessageProperties GetIncomingMessageProperties<TMessage>(this TMessage message) where TMessage : class
        {
            if(message == null) throw new ArgumentNullException(nameof(message));

            IMessagePropertiesProvider configuredMessagePropertyProvider = MessagePropertiesProvider.Default;

            if(configuredMessagePropertyProvider == null)
            {
                throw new InvalidOperationException($"No {nameof(IMessagePropertiesProvider)} has been configured.");
            }

            return configuredMessagePropertyProvider.GetIncomingMessageProperties(message);
        }
    }
}
