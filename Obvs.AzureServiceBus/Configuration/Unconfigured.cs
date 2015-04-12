using System;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Obvs.AzureServiceBus.Infrastructure;

namespace Obvs.AzureServiceBus.Configuration
{
    internal sealed class UnconfiguredMessageSender<TMessage> : IMessageSender
    {
        public static readonly UnconfiguredMessageSender<TMessage> Default = new UnconfiguredMessageSender<TMessage>();


        public Task SendAsync(BrokeredMessage brokeredMessage)
        {
            throw new InvalidOperationException(string.Format("An attempt was made to send an unconfigured message of type {0}. You must configure the provider with a mapping for this type if you want to be able to send it.", typeof(TMessage).Name));
        }

        public void Dispose()
        {
        }
    }

    internal sealed class UnconfiguredMessageReceiver<TMessage> : IMessageReceiver
    {
        public static readonly UnconfiguredMessageReceiver<TMessage> Default = new UnconfiguredMessageReceiver<TMessage>();

        public ReceiveMode Mode
        {
            get
            {
                return ReceiveMode.PeekLock;
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
            throw new InvalidOperationException(string.Format("An attempt was made to receive an unconfigured message of type {0}. You must configure the provider with a mapping for this type if you want to be able to receive it.", typeof(TMessage).Name));
        }

        public void Dispose()
        {
        }
    }
}
