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

        public MessageTypePathMappingDetails(Type messageType, string path, MessagingEntityType messagingEntityType)
            : this(messageType, path, messagingEntityType, createIfDoesntExist: false, isTemporary: false)
        {
        }

        public MessageTypePathMappingDetails(Type messageType, string path, MessagingEntityType messagingEntityType, bool createIfDoesntExist, bool isTemporary)
        {
            _messageType = messageType;
            _path = path;
            _messagingEntityType = messagingEntityType;
            _createIfDoesntExist = createIfDoesntExist;
            _isTemporary = isTemporary;
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
            if(mappingDetails.CreateIfDoesntExist)
            {
                EnsureMessagingEntityExistsInternal(
                    mappingDetails,
                    alreadyExistsAction: () =>
                    {
                        if(mappingDetails.IsTemporary)
                        {
                            throw new Exception(string.Format("A messaging entity with a path of \"{0}\" of type {1} already exists. To ensure intent and keep your data safe the framwork cannot recrate it as temporary. You must manually delete the entity if you want to specify it as a temporary again.", mappingDetails.Path, mappingDetails.MessagingEntityType));
                        }
                    },
                    doesntAlreadyExistAction: () =>
                    {
                        // TODO: log
                    });

            }
            else
            {
                EnsureMessagingEntityExistsInternal(
                    mappingDetails,
                    alreadyExistsAction: () =>
                    {
                        // TODO: log
                    },
                    doesntAlreadyExistAction: () =>
                    {
                        // TODO: log

                        if(!mappingDetails.IsTemporary)
                        {
                            throw new Exception(string.Format("A messaging entity with a path of \"{0}\" of type {1} does not exist.", mappingDetails.Path, mappingDetails.MessagingEntityType));
                        }
                    });
            }
        }

        private void EnsureMessagingEntityExistsInternal(MessageTypePathMappingDetails mappingDetails, Action alreadyExistsAction, Action doesntAlreadyExistAction)
        {
            if(!_verifiedExistingMessagingEntities.Contains(mappingDetails))
            {
                Func<bool> exists;
                Action create;

                switch(mappingDetails.MessagingEntityType)
                {
                    case MessagingEntityType.Queue:
                        exists = () => _namespaceManager.QueueExists(mappingDetails.Path);
                        create = () => _namespaceManager.CreateQueue(mappingDetails.Path);

                        break;

                    case MessagingEntityType.Topic:
                        exists = () => _namespaceManager.TopicExists(mappingDetails.Path);
                        create = () => _namespaceManager.CreateTopic(mappingDetails.Path);

                        break;

                    case MessagingEntityType.Subscription:
                        string[] parts = mappingDetails.Path.Split('/');

                        exists = () => _namespaceManager.SubscriptionExists(parts[0], parts[2]);
                        create = () => _namespaceManager.CreateSubscription(parts[0], parts[2]);

                        break;

                    default:
                        throw new NotSupportedException(string.Format("Unsupported messaging entity type, {0}, requested for creation (path {1}).", mappingDetails.MessagingEntityType, mappingDetails.Path));
                }

                if(exists())
                {
                    alreadyExistsAction();
                }
                else
                {
                    doesntAlreadyExistAction();

                    create();
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
