using System;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Obvs.AzureServiceBus.Configuration;

namespace Obvs.AzureServiceBus.Infrastructure
{
    public interface IMessageReceiver : IDisposable
    {
        MessageReceiveMode Mode
        {
            get;
        }

        bool IsClosed
        {
            get;
        }

        Task<BrokeredMessage> ReceiveAsync();
    }
}
