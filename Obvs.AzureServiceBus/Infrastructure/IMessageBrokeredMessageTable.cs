using System;
using System.Runtime.CompilerServices;
using Microsoft.ServiceBus.Messaging;

namespace Obvs.AzureServiceBus.Infrastructure
{
    internal interface IMessageBrokeredMessageTable
    {
        void SetBrokeredMessageForMessage(object message, BrokeredMessage brokeredMessage);

        BrokeredMessage GetBrokeredMessageForMessage(object message);

        void RemoveBrokeredMessageForMessage(object message);
    }

    internal sealed class MessageBrokeredMessageTable
    {
        public static IMessageBrokeredMessageTable Default = new DefaultMessageBrokeredMessageTable();
    }

    internal sealed class DefaultMessageBrokeredMessageTable : IMessageBrokeredMessageTable
    {
        ConditionalWeakTable<object, BrokeredMessage> _innerTable = new ConditionalWeakTable<object, BrokeredMessage>();

        public void SetBrokeredMessageForMessage(object message, BrokeredMessage brokeredMessage)
        {
            if(message == null) throw new ArgumentNullException(nameof(message));
            if(brokeredMessage == null) throw new ArgumentNullException(nameof(brokeredMessage));

            _innerTable.Add(message, brokeredMessage);
        }

        public BrokeredMessage GetBrokeredMessageForMessage(object message)
        {
            BrokeredMessage brokeredMessage;

            _innerTable.TryGetValue(message, out brokeredMessage);

            return brokeredMessage;
        }

        public void RemoveBrokeredMessageForMessage(object message)
        {
            _innerTable.Remove(message);
        }
    }
}
