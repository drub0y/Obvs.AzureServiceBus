using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.ServiceBus.Messaging;

namespace Obvs.AzureServiceBus.Infrastructure
{
    public interface IMessageReceiver : IDisposable
    {
        ReceiveMode Mode
        {
            get;
        }

        void OnMessage(Action<BrokeredMessage> callback, OnMessageOptions options);
    }
}
