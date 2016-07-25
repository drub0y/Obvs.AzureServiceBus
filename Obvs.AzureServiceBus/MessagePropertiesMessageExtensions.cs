using System;
using Obvs.AzureServiceBus.Infrastructure;

namespace Obvs.AzureServiceBus
{
    public static class MessagePropertiesMessageExtensions
    {
        public static IOutgoingMessageProperties GetOutgoingMessageProperties<TMessage>(this TMessage message) where TMessage : class => ValidateMessageAndGetConfiguredMessagePropertyProvider(message).GetOutgoingMessageProperties(message);

        public static IIncomingMessageProperties GetIncomingMessageProperties<TMessage>(this TMessage message) where TMessage : class => ValidateMessageAndGetConfiguredMessagePropertyProvider(message).GetIncomingMessageProperties(message);

        private static IMessagePropertiesProvider ValidateMessageAndGetConfiguredMessagePropertyProvider<TMessage>(TMessage message) where TMessage : class
        {
            if(message == null) throw new ArgumentNullException(nameof(message));

            IMessagePropertiesProvider configuredMessagePropertyProvider = MessagePropertiesProvider.ConfiguredInstance;

            if(configuredMessagePropertyProvider == null)
            {
                throw new InvalidOperationException($"No {nameof(IMessagePropertiesProvider)} has been configured.");
            }

            return configuredMessagePropertyProvider;
        }
    }
}
