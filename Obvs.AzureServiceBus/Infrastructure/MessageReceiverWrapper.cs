using System;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Obvs.AzureServiceBus.Configuration;

namespace Obvs.AzureServiceBus.Infrastructure
{
    internal sealed class MessageReceiverWrapper : IMessageReceiver
    {
        private readonly Type _supportedMessageType;
        private readonly MessageReceiver _messageReceiver;

        public MessageReceiverWrapper(Type supportedMessageType, MessageReceiver messageReceiver)
        {
            _supportedMessageType = supportedMessageType;
            _messageReceiver = messageReceiver;
        }

        public Type SupportedMessageType
        {
            get
            {
                return _supportedMessageType;
            }
        }

        public MessageReceiveMode Mode
        {
            get
            {
                return MessageReceiveModeTranslator.TranslateAzureServiceBusReceiveModeValueToConfigurationValue(_messageReceiver.Mode);
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
