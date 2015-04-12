using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace Obvs.AzureServiceBus.Infrastructure
{
    public interface IMessageReceiver : IDisposable
    {
        ReceiveMode Mode
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
