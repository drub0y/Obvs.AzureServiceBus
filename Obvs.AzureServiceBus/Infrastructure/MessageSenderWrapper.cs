using System;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace Obvs.AzureServiceBus.Infrastructure
{
    internal sealed class MessageSenderWrapper : IMessageSender
    {
        MessageSender _messageSender;
        Type _supportedMessageType;

        public MessageSenderWrapper(Type supportedMessageType, MessageSender messageSender)
        {
            if(supportedMessageType == null) throw new ArgumentNullException("supportedMessageType");
            if(messageSender == null) throw new ArgumentNullException("messageSender");

            _supportedMessageType = supportedMessageType;
            _messageSender = messageSender;
        }

        public Type SupportedMessageType
        {
            get
            {
                return _supportedMessageType;
            }
        }

        public Task SendAsync(BrokeredMessage brokeredMessage)
        {
            return _messageSender.SendAsync(brokeredMessage);
        }

        public void Dispose()
        {
            _messageSender.Close();
        }
    }

}
