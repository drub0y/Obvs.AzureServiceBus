using System;
using Microsoft.ServiceBus.Messaging;

namespace Obvs.AzureServiceBus.Infrastructure
{
    internal static class MessagePropertiesProvider
    {
        public static IMessagePropertiesProvider Default;
    }

    internal interface IMessagePropertiesProvider
    {
        IIncomingMessageProperties GetIncomingMessageProperties(object message);
        IOutgoingMessageProperties GetOutgoingMessageProperties(object message);
    }

    internal sealed class DefaultBrokeredMessagePropertiesProvider : IMessagePropertiesProvider
    {
        private IMessageBrokeredMessageTable _messageBrokeredMessageTable;

        public DefaultBrokeredMessagePropertiesProvider(IMessageBrokeredMessageTable messageBrokeredMessageTable)
        {
            _messageBrokeredMessageTable = messageBrokeredMessageTable;
        }

        public IIncomingMessageProperties GetIncomingMessageProperties(object message) => new DefaultBrokeredMessageIncomingMessageProperties(_messageBrokeredMessageTable.GetBrokeredMessageForMessage(message));

        public IOutgoingMessageProperties GetOutgoingMessageProperties(object message) => new DefaultBrokeredMessageOutgoingMessageProperties(_messageBrokeredMessageTable.GetBrokeredMessageForMessage(message));
    }

    public interface IIncomingMessageProperties
    {
        int DeliveryCount
        {
            get;
        }
    }

    public interface IOutgoingMessageProperties
    {
        DateTime ScheduledEnqueueTimeUtc
        {
            get;
            set;
        }

        TimeSpan TimeToLive
        {
            get;
            set;
        }
    }

    internal struct DefaultBrokeredMessageIncomingMessageProperties : IIncomingMessageProperties
    {
        private BrokeredMessage _brokeredMessage;

        public DefaultBrokeredMessageIncomingMessageProperties(BrokeredMessage brokeredMessage)
        {
            _brokeredMessage = brokeredMessage;
        }

        public int DeliveryCount
        {
            get
            {
                return _brokeredMessage.DeliveryCount;
            }
        }
    }

    internal struct DefaultBrokeredMessageOutgoingMessageProperties : IOutgoingMessageProperties
    {
        private BrokeredMessage _brokeredMessage;

        public DefaultBrokeredMessageOutgoingMessageProperties(BrokeredMessage brokeredMessage)
        {
            _brokeredMessage = brokeredMessage;
        }

        public DateTime ScheduledEnqueueTimeUtc
        {
            get
            {
                return _brokeredMessage.ScheduledEnqueueTimeUtc;
            }
            set
            {
                _brokeredMessage.ScheduledEnqueueTimeUtc = value;
            }
        }

        public TimeSpan TimeToLive
        {
            get
            {
                return _brokeredMessage.TimeToLive;
            }

            set
            {
                _brokeredMessage.TimeToLive = value;
            }
        }
    }
}
