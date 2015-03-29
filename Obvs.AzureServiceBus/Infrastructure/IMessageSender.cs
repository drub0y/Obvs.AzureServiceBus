using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace Obvs.AzureServiceBus.Infrastructure
{
    internal interface IMessageSender
    {
        void Send(BrokeredMessage brokeredMessage);

        Task SendAsync(BrokeredMessage brokeredMessage);
    }
}
