using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace Obvs.AzureServiceBus.Configuration
{
    public enum MessagingEntityType
    {
        Queue = 0,
        Topic = 1,
        Subscription = 2
    }

    public struct MessageTypePathMappingDetails
    {
        private readonly Type _messageType;
        private readonly string _path;
        private readonly MessagingEntityType _messagingEntityType;
        private readonly ReceiveMode _receiveMode;

        public MessageTypePathMappingDetails(Type messageType, string path, MessagingEntityType messagingEntityType)
            : this(messageType, path, messagingEntityType, ReceiveMode.PeekLock)
        {
        }

        public MessageTypePathMappingDetails(Type messageType, string path, MessagingEntityType messagingEntityType, ReceiveMode receiveMode)
        {
            _messageType = messageType;
            _path = path;
            _messagingEntityType = messagingEntityType;
            _receiveMode = receiveMode;
        }

        public Type MessageType
        {
            get
            {
                return _messageType;
            }
        }

        public string Path
        {
            get
            {
                return _path;
            }
        }

        public ReceiveMode ReceiveMode
        {
            get
            {
                return _receiveMode;
            }
        }

        public MessagingEntityType MessagingEntityType
        {
            get
            {
                return _messagingEntityType;
            }
        }
    }
    
    internal sealed class MessageClientEntityFactory
    {
        private readonly MessagingFactory _messagingFactory;
        private readonly List<MessageTypePathMappingDetails> _messageTypePathMappings;

        public MessageClientEntityFactory(string connectionString, MessagingFactorySettings settings, List<MessageTypePathMappingDetails> messageTypePathMappings)
        {
            _messagingFactory = MessagingFactory.CreateFromConnectionString(connectionString);
            _messageTypePathMappings = messageTypePathMappings;
        }

        public MessageReceiver CreateMessageReceiver<TMessage>()
        {
            MessageTypePathMappingDetails mappingDetails = GetMappingDetails<TMessage>(MessagingEntityType.Queue, MessagingEntityType.Subscription);

            MessageReceiver messageReceiver = _messagingFactory.CreateMessageReceiver(mappingDetails.Path, mappingDetails.ReceiveMode);

            return messageReceiver;
        }

        public MessageSender CreateMessageSender<TMessage>()
        {
            MessageTypePathMappingDetails mappingDetails = GetMappingDetails<TMessage>(MessagingEntityType.Queue, MessagingEntityType.Topic);

            MessageSender messageSender = _messagingFactory.CreateMessageSender(mappingDetails.Path);

            return messageSender;
        }

        private MessageTypePathMappingDetails GetMappingDetails<TMessage>(params MessagingEntityType[] allowedEntityTypes)
        {
            MessageTypePathMappingDetails mappingDetails;

            try
            {
                mappingDetails = (from mtpm in _messageTypePathMappings
                                  join aet in allowedEntityTypes on mtpm.MessagingEntityType equals aet
                                  where mtpm.MessageType == typeof(TMessage)
                                  select mtpm).SingleOrDefault();
            }
            catch(InvalidOperationException)
            {
                throw new Exception(string.Format("More than one path mapping exist for message type {0} for allowed entity types {1}", typeof(TMessage).Name, string.Join(", ", allowedEntityTypes)));
            }

            if(mappingDetails.Equals(default(MessageTypePathMappingDetails)))
            {
                throw new Exception(string.Format("Missing path mapping for message type {0} for allowed entity types {1}", typeof(TMessage).Name, string.Join(", ", allowedEntityTypes)));
            }

            return mappingDetails;
        }
    }
}
