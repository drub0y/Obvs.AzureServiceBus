using System;
using Obvs.AzureServiceBus.Infrastructure;

namespace Obvs.AzureServiceBus
{
    public interface IMessageClientEntityFactory
    {
        IMessageReceiver CreateMessageReceiver(Type messageType);
        IMessageSender CreateMessageSender(Type messageType);
    }
}