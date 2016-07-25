using System;
using System.Runtime.CompilerServices;
using Microsoft.ServiceBus.Messaging;

namespace Obvs.AzureServiceBus.Infrastructure
{
    public interface IMessagePropertiesProvider
    {
        IIncomingMessageProperties GetIncomingMessageProperties(object message);
        IOutgoingMessageProperties GetOutgoingMessageProperties(object message);
    }

    internal static class MessagePropertiesProvider
    {
        private static IMessagePropertiesProvider Instance;

        public static IMessagePropertiesProvider ConfiguredInstance
        {
            get
            {
                if(Instance == null)
                {
                    UseDefault();
                }

                return Instance;
            }
        }

        public static void Use(IMessagePropertiesProvider messagePropertiesProvider)
        {
            Instance = messagePropertiesProvider;
        }

        public static void UseDefault() => Use(new DefaultBrokeredMessagePropertiesProvider(MessageBrokeredMessageTable.ConfiguredInstance));

        public static void UseFakeMessagePropertiesProvider() => Use(new FakeMessagePropertiesProvider());
    }

    internal class FakeMessagePropertiesProvider : IMessagePropertiesProvider
    {
        private static readonly ConditionalWeakTable<object, Tuple<FakeIncomingMessageProperties, FakeOutgoingMessageProperties>> _trackedMessagePropertiesTable = new ConditionalWeakTable<object, Tuple<FakeIncomingMessageProperties, FakeOutgoingMessageProperties>>();

        public IIncomingMessageProperties GetIncomingMessageProperties(object message) => GetTrackedMessageProperties(message).Item1;

        public IOutgoingMessageProperties GetOutgoingMessageProperties(object message) => GetTrackedMessageProperties(message).Item2;

        private Tuple<FakeIncomingMessageProperties, FakeOutgoingMessageProperties> GetTrackedMessageProperties(object message)
        {
            Tuple<FakeIncomingMessageProperties, FakeOutgoingMessageProperties> messageProperties;

            if(!_trackedMessagePropertiesTable.TryGetValue(message, out messageProperties))
            {
                messageProperties = new Tuple<FakeIncomingMessageProperties, FakeOutgoingMessageProperties>(new FakeIncomingMessageProperties(), new FakeOutgoingMessageProperties());

                _trackedMessagePropertiesTable.Add(message, messageProperties);
            }

            return messageProperties;
        }

        private sealed class FakeOutgoingMessageProperties : IOutgoingMessageProperties
        {
            public DateTime ScheduledEnqueueTimeUtc
            {
                get;
                set;
            }

            public TimeSpan TimeToLive
            {
                get;
                set;
            }
        }

        private sealed class FakeIncomingMessageProperties : IIncomingMessageProperties
        {
            public int DeliveryCount
            {
                get
                {
                    return 1;
                }
            }
        }
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

    internal sealed class DefaultBrokeredMessageIncomingMessageProperties : IIncomingMessageProperties
    {
        private readonly BrokeredMessage _brokeredMessage;

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

    internal sealed class DefaultBrokeredMessageOutgoingMessageProperties : IOutgoingMessageProperties
    {
        private readonly BrokeredMessage _brokeredMessage;

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
