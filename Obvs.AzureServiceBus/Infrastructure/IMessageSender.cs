using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace Obvs.AzureServiceBus.Infrastructure
{
    internal interface IMessageSender
    {
        Task SendAsync(BrokeredMessage brokeredMessage);
    }
}
