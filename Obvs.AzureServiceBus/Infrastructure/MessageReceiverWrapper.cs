using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace Obvs.AzureServiceBus.Infrastructure
{
    internal sealed class MessageReceiverWrapper : IMessageReceiver
    {
        private readonly MessageReceiver _messageReceiver;

        public MessageReceiverWrapper(MessageReceiver messageReceiver)
        {
            _messageReceiver = messageReceiver;
        }

        public ReceiveMode Mode
        {
            get
            {
                return _messageReceiver.Mode;
            }
        }

        public bool IsClosed
        {
            get
            {
                return _messageReceiver.IsClosed;
            }
        }

        public Task<BrokeredMessage> ReceiveAsync()
        {
            return _messageReceiver.ReceiveAsync();
        }

        public void Dispose()
        {
            _messageReceiver.Close();
        }
    }
}
