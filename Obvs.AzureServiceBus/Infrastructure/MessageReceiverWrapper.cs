using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    }
}
