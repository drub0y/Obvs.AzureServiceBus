using System;
using Obvs.AzureServiceBus.Infrastructure;

namespace Obvs.AzureServiceBus
{
    public interface IMessagingEntityFactory
    {
        IMessageReceiver CreateMessageReceiver(Type messageType);
        IMessageSender CreateMessageSender(Type messageType);
    }
}