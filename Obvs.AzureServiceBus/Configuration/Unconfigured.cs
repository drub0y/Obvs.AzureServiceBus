using System;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Obvs.AzureServiceBus.Infrastructure;

namespace Obvs.AzureServiceBus.Configuration
{
    internal sealed class UnconfiguredMessageSender : IMessageSender
    {
        private readonly Type _messageType;

        public UnconfiguredMessageSender(Type messageType)
        {
            _messageType = messageType;
        }

        public Type SupportedMessageType
        {
            get
            {
                return _messageType;
            }
        }

        public Task SendAsync(BrokeredMessage brokeredMessage)
        {
            throw new InvalidOperationException(string.Format("An attempt was made to send an unconfigured message of type {0}. You must configure the provider with a mapping for this type if you want to be able to send it.", _messageType.Name));
        }

        public void Dispose()
        {
        }
    }

    internal sealed class UnconfiguredMessageReceiver : IMessageReceiver
    {
        private readonly Type _messageType;

        public UnconfiguredMessageReceiver(Type messageType)
        {
            _messageType = messageType;
        }

        public Type SupportedMessageType
        {
            get
            {
                return _messageType;
            }
        }

        public MessageReceiveMode Mode
        {
            get
            {
                return MessageReceiveMode.ReceiveAndDelete;
            }
        }

        public bool IsClosed
        {
            get
            {
                return false;
            }
        }

        public Task<BrokeredMessage> ReceiveAsync()
        {
            throw new InvalidOperationException(string.Format("An attempt was made to receive an unconfigured message of type {0}. You must configure the provider with a mapping for this type if you want to be able to receive it.", _messageType.Name));
        }

        public void Dispose()
        {
        }
    }
}
