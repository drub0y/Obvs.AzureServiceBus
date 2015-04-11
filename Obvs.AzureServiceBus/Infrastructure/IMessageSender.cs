using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace Obvs.AzureServiceBus.Infrastructure
{
    public interface IMessageSender
    {
        Task SendAsync(BrokeredMessage brokeredMessage);
    }
}
