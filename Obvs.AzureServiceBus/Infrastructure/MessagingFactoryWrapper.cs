using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.ServiceBus.Messaging;

namespace Obvs.AzureServiceBus.Infrastructure
{
    internal sealed class MessagingFactoryWrapper : IMessagingFactory
    {
        private readonly MessagingFactory _messagingFactory;

        public MessagingFactoryWrapper(MessagingFactory messagingFactory)
        {
            _messagingFactory = messagingFactory;
        }

        public IMessageSender CreateMessageSender(string entityPath)
        {
            return new MessageSenderWrapper(_messagingFactory.CreateMessageSender(entityPath));
        }

        public IMessageReceiver CreateMessageReceiver(string entityPath, ReceiveMode receiveMode)
        {
            return new MessageReceiverWrapper(_messagingFactory.CreateMessageReceiver(entityPath, receiveMode));
        }
    }
}
