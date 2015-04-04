using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
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
        private readonly bool _isDynamic;

        public MessageTypePathMappingDetails(Type messageType, string path, MessagingEntityType messagingEntityType)
            : this(messageType, path, messagingEntityType, ReceiveMode.PeekLock, false)
        {
        }

        public MessageTypePathMappingDetails(Type messageType, string path, MessagingEntityType messagingEntityType, ReceiveMode receiveMode, bool isDynamic)
        {
            _messageType = messageType;
            _path = path;
            _messagingEntityType = messagingEntityType;
            _receiveMode = receiveMode;
            _isDynamic = isDynamic;
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

        public bool IsDynamic
        {
            get
            {
                return _isDynamic;
            }
        }
    }
    
    internal sealed class MessageClientEntityFactory
    {
        private readonly NamespaceManager _namespaceManager;
        private readonly MessagingFactory _messagingFactory;
        private readonly List<MessageTypePathMappingDetails> _messageTypePathMappings;

        public MessageClientEntityFactory(string connectionString, MessagingFactorySettings settings, List<MessageTypePathMappingDetails> messageTypePathMappings)
        {
            _namespaceManager = NamespaceManager.CreateFromConnectionString(connectionString);
            _messagingFactory = MessagingFactory.Create(_namespaceManager.Address, _namespaceManager.Settings.TokenProvider);
            _messageTypePathMappings = messageTypePathMappings;
        }

        public MessageReceiver CreateMessageReceiver<TMessage>()
        {
            MessageTypePathMappingDetails mappingDetails = GetMappingDetails<TMessage>(MessagingEntityType.Queue, MessagingEntityType.Subscription);

            string finalMessagingEntityPath;

            if(mappingDetails.IsDynamic)
            {
                finalMessagingEntityPath = CreateDynamicMessagingEntity(mappingDetails);
            }
            else
            {
                finalMessagingEntityPath = mappingDetails.Path;
            }

            MessageReceiver messageReceiver = _messagingFactory.CreateMessageReceiver(mappingDetails.Path, mappingDetails.ReceiveMode);

            return messageReceiver;
        }

        public MessageSender CreateMessageSender<TMessage>()
        {
            MessageTypePathMappingDetails mappingDetails = GetMappingDetails<TMessage>(MessagingEntityType.Queue, MessagingEntityType.Topic);

            string finalMessagingEntityPath;

            if(mappingDetails.IsDynamic)
            {
                finalMessagingEntityPath = CreateDynamicMessagingEntity(mappingDetails);
            }
            else
            {
                finalMessagingEntityPath = mappingDetails.Path;
            }

            MessageSender messageSender = _messagingFactory.CreateMessageSender(finalMessagingEntityPath);

            return messageSender;
        }

        private string CreateDynamicMessagingEntity(MessageTypePathMappingDetails mappingDetails)
        {
            Process currentProcess = Process.GetCurrentProcess();

            string dynamicEntityName = string.Format("Obvs-DynSub-{0}-{1}-{2}", mappingDetails.MessageType.Name, Environment.MachineName, currentProcess.Id);

            string path;
            
            switch(mappingDetails.MessagingEntityType)
            {
                case MessagingEntityType.Subscription:
                    string topicPath = mappingDetails.Path;

                    _namespaceManager.CreateSubscription(new SubscriptionDescription(topicPath, dynamicEntityName)
                    {
                        AutoDeleteOnIdle = TimeSpan.FromMinutes(5)
                    });
                    
                    path = topicPath + "/subcriptions/" + dynamicEntityName;

                    break;

                default:
                    throw new NotSupportedException(string.Format("Unsupported messaging entity type for dynamic creation: {0}", mappingDetails.MessagingEntityType));
            }

            return path;
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
