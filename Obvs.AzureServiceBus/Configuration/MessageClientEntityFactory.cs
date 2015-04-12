using System;
using System.Collections.Generic;
using System.Linq;
using Obvs.AzureServiceBus.Infrastructure;

namespace Obvs.AzureServiceBus.Configuration
{
    public enum MessagingEntityType
    {
        Queue = 0,
        Topic = 1,
        Subscription = 2
    }

    public class MessageTypePathMappingDetails
    {
        private readonly Type _messageType;
        private readonly string _path;
        private readonly MessagingEntityType _messagingEntityType;
        private readonly bool _isTemporary;
        private readonly bool _createIfDoesntExist;
        private readonly bool _canDeleteTemporaryIfAlreadyExists;

        public MessageTypePathMappingDetails(Type messageType, string path, MessagingEntityType messagingEntityType)
            : this(messageType, path, messagingEntityType, createIfDoesntExist: false)
        {
        }

        public MessageTypePathMappingDetails(Type messageType, string path, MessagingEntityType messagingEntityType, bool createIfDoesntExist)
            : this(messageType, path, messagingEntityType, createIfDoesntExist, isTemporary: false, canDeleteTemporaryIfAlreadyExists: false)
        {
        }

        public MessageTypePathMappingDetails(Type messageType, string path, MessagingEntityType messagingEntityType, bool createIfDoesntExist, bool isTemporary, bool canDeleteTemporaryIfAlreadyExists)
        {
            _messageType = messageType;
            _path = path;
            _messagingEntityType = messagingEntityType;
            _createIfDoesntExist = createIfDoesntExist;
            _isTemporary = isTemporary;
            _canDeleteTemporaryIfAlreadyExists = canDeleteTemporaryIfAlreadyExists;
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

        public MessagingEntityType MessagingEntityType
        {
            get
            {
                return _messagingEntityType;
            }
        }

        public bool CreateIfDoesntExist
        {
            get
            {
                return _createIfDoesntExist;
            }
        }

        public bool IsTemporary
        {
            get
            {
                return _isTemporary;
            }
        }

        public bool CanDeleteTemporaryIfAlreadyExists
        {
            get
            {
                return _canDeleteTemporaryIfAlreadyExists;
            }
        }

        public sealed class PathComparer : IEqualityComparer<MessageTypePathMappingDetails>
        {
            public bool Equals(MessageTypePathMappingDetails x, MessageTypePathMappingDetails y)
            {
                return StringComparer.OrdinalIgnoreCase.Equals(x, y);
            }

            public int GetHashCode(MessageTypePathMappingDetails obj)
            {
                return obj.Path.GetHashCode();
            }
        }
    }

    internal sealed class MessageClientEntityFactory
    {
        private readonly INamespaceManager _namespaceManager;
        private readonly IMessagingFactory _messagingFactory;
        private readonly List<MessageTypePathMappingDetails> _messageTypePathMappings;
        private readonly HashSet<MessageTypePathMappingDetails> _verifiedExistingMessagingEntities = new HashSet<MessageTypePathMappingDetails>(new MessageTypePathMappingDetails.PathComparer());

        public MessageClientEntityFactory(INamespaceManager namespaceManager, IMessagingFactory messagingFactory, List<MessageTypePathMappingDetails> messageTypePathMappings)
        {
            if(namespaceManager == null) throw new ArgumentNullException("namespaceManager");
            if(messagingFactory == null) throw new ArgumentNullException("messagingFactory");
            if(messageTypePathMappings == null) throw new ArgumentNullException("messageTypePathMappings");
            if(messageTypePathMappings.Count == 0) throw new ArgumentException("No message types have been mapped.", "messageTypePathMappings");

            _namespaceManager = namespaceManager;
            _messagingFactory = messagingFactory;
            _messageTypePathMappings = messageTypePathMappings;
        }

        public IMessageReceiver CreateMessageReceiver<TMessage>()
        {
            MessageTypePathMappingDetails mappingDetails = GetMappingDetails<TMessage>(MessagingEntityType.Queue, MessagingEntityType.Subscription);

            IMessageReceiver messageReceiver;

            if(mappingDetails != null)
            {
                EnsureMessagingEntityExists(mappingDetails);
                
                messageReceiver = _messagingFactory.CreateMessageReceiver(mappingDetails.Path);
            }
            else
            {
                // TODO: log

                messageReceiver = UnconfiguredMessageReceiver<TMessage>.Default;
            }

            return messageReceiver;
        }

        public IMessageSender CreateMessageSender<TMessage>()
        {
            MessageTypePathMappingDetails mappingDetails = GetMappingDetails<TMessage>(MessagingEntityType.Queue, MessagingEntityType.Topic);

            IMessageSender messageSender;
            
            if(mappingDetails != null)
            {
                EnsureMessagingEntityExists(mappingDetails);
                
                messageSender = _messagingFactory.CreateMessageSender(mappingDetails.Path);
            }
            else
            {
                // TODO: log

                messageSender = UnconfiguredMessageSender<TMessage>.Default;
            }

            return messageSender;
        }

        private void EnsureMessagingEntityExists(MessageTypePathMappingDetails mappingDetails)
        {
            if(!_verifiedExistingMessagingEntities.Contains(mappingDetails))
            {
                Func<bool> exists;
                Action create;
                Action delete;

                string path = mappingDetails.Path;

                switch(mappingDetails.MessagingEntityType)
                {
                    case MessagingEntityType.Queue:
                        exists = () => _namespaceManager.QueueExists(path);
                        create = () => _namespaceManager.CreateQueue(path);
                        delete = () => _namespaceManager.DeleteQueue(path);

                        break;

                    case MessagingEntityType.Topic:
                        exists = () => _namespaceManager.TopicExists(path);
                        create = () => _namespaceManager.CreateTopic(path);
                        delete = () => _namespaceManager.DeleteTopic(path);

                        break;

                    case MessagingEntityType.Subscription:
                        string[] parts = path.Split('/');
                        string topicPath = parts[0];
                        string subscriptionName = parts[2];

                        exists = () => _namespaceManager.SubscriptionExists(topicPath, subscriptionName);
                        create = () => _namespaceManager.CreateSubscription(topicPath, subscriptionName);
                        delete = () => _namespaceManager.DeleteSubscription(topicPath, subscriptionName);

                        break;

                    default:
                        throw new NotSupportedException(string.Format("Unsupported messaging entity type, {0}, requested for creation (path {1}).", mappingDetails.MessagingEntityType, mappingDetails.Path));
                }

                if(exists())
                {
                    if(mappingDetails.IsTemporary)
                    {
                        if(!mappingDetails.CanDeleteTemporaryIfAlreadyExists)
                        {
                            throw new MessagingEntityAlreadyExistsException(mappingDetails.Path, mappingDetails.MessagingEntityType);
                        }

                        try
                        {
                            delete();
                        }
                        catch(UnauthorizedAccessException exception)
                        {
                            throw new UnauthorizedAccessException(string.Format("Unable to delete temporary messaging that already exists at path \"{0}\" due to insufficient access. Make sure the policy being used has 'Manage' permission for the namespace.", mappingDetails.Path), exception);
                        }
                    };
                }
                else if(!mappingDetails.CreateIfDoesntExist)
                {
                    throw new MessagingEntityDoesNotAlreadyExistException(mappingDetails.Path, mappingDetails.MessagingEntityType);
                }

                try
                {
                    create();
                }
                catch(UnauthorizedAccessException exception)
                {
                    throw new UnauthorizedAccessException(string.Format("Unable to create messaging entity at path \"{0}\" due to insufficient access. Make sure the policy being used has 'Manage' permission for the namespace.", mappingDetails.Path), exception);
                }

                _verifiedExistingMessagingEntities.Add(mappingDetails);
            }
        }

        private MessageTypePathMappingDetails GetMappingDetails<TMessage>(params MessagingEntityType[] expectedEntityTypes)
        {
            MessageTypePathMappingDetails mappingDetails;

            try
            {
                mappingDetails = (from mtpm in _messageTypePathMappings
                                  join eet in expectedEntityTypes on mtpm.MessagingEntityType equals eet
                                  where mtpm.MessageType == typeof(TMessage)
                                  select mtpm).SingleOrDefault();
            }
            catch(InvalidOperationException exception)
            {
                throw new MoreThanOneMappingExistsForMessageTypeException(typeof(TMessage), expectedEntityTypes, exception);
            }

            return mappingDetails;
        }
    }
}
