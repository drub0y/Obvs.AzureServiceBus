using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Obvs.AzureServiceBus.Configuration;

namespace Obvs.AzureServiceBus.Infrastructure
{
    public interface IMessagingFactory
    {
        IMessageSender CreateMessageSender(string entityPath);

        IMessageReceiver CreateMessageReceiver(string entityPath, MessageReceiveMode receiveMode);
    }
}
