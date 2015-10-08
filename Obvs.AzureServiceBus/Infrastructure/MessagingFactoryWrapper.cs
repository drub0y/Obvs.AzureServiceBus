using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.ServiceBus.Messaging;
using Obvs.AzureServiceBus.Configuration;

namespace Obvs.AzureServiceBus.Infrastructure
{
    internal sealed class MessagingFactoryWrapper : IMessagingFactory
    {
        private readonly MessagingFactory _messagingFactory;

        public MessagingFactoryWrapper(MessagingFactory messagingFactory)
        {
            _messagingFactory = messagingFactory;
        }

        public IMessageSender CreateMessageSender(Type messageType, string entityPath)
        {
            return new MessageSenderWrapper(messageType, _messagingFactory.CreateMessageSender(entityPath));
        }

        public IMessageReceiver CreateMessageReceiver(Type messageType, string entityPath, MessageReceiveMode receiveMode)
        {
            return new MessageReceiverWrapper(messageType, _messagingFactory.CreateMessageReceiver(entityPath, MessageReceiveModeTranslator.TranslateReceiveModeConfigurationValueToAzureServiceBusValue(receiveMode)));
        }       
    }
}
