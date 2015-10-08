using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Obvs.AzureServiceBus.Configuration;

namespace Obvs.AzureServiceBus.Infrastructure
{
    public interface IMessagingFactory
    {
        IMessageSender CreateMessageSender(Type messageType, string entityPath);

        IMessageReceiver CreateMessageReceiver(Type messageType, string entityPath, MessageReceiveMode receiveMode);
    }
}
