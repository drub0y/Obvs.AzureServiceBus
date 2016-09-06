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
        private static readonly IMessageBrokeredMessageTable Instance = new DefaultMessageBrokeredMessageTable();

        public static IMessageBrokeredMessageTable ConfiguredInstance
        {
            get
            {
                return Instance;
            }
        }
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
            BrokeredMessage result;

            _innerTable.TryGetValue(message, out result);

            return result;
        }

        public void RemoveBrokeredMessageForMessage(object message) => _innerTable.Remove(message);
    }
}
