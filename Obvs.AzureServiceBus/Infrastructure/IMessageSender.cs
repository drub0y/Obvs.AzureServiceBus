using System;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace Obvs.AzureServiceBus.Infrastructure
{
    public interface IMessageSender : IDisposable
    {
        Type SupportedMessageType
        {
            get;
        }

        Task SendAsync(BrokeredMessage brokeredMessage);
    }
}
