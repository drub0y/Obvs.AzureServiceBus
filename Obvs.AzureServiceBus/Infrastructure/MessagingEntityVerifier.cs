using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Obvs.AzureServiceBus.Configuration;

namespace Obvs.AzureServiceBus.Infrastructure
{
    internal class MessagingEntityVerifier : IMessagingEntityVerifier
    {
        private readonly INamespaceManager _namespaceManager;
        private readonly HashSet<MessageTypeMessagingEntityMappingDetails> _verifiedExistingMessagingEntities = new HashSet<MessageTypeMessagingEntityMappingDetails>(new MessageTypeMessagingEntityMappingDetails.MessagingEntityTypeAndPathComparer());

        public MessagingEntityVerifier(INamespaceManager namespaceManager)
        {
            _namespaceManager = namespaceManager;
        }

        public void EnsureMessagingEntitiesExist(IEnumerable<MessageTypeMessagingEntityMappingDetails> messageTypePathMappings)
        {
            foreach(MessageTypeMessagingEntityMappingDetails mappingDetails in messageTypePathMappings)
            {
                EnsureMessagingEntityExists(mappingDetails, messageTypePathMappings);
            }
        }

        private void EnsureMessagingEntityExists(MessageTypeMessagingEntityMappingDetails mappingDetails, IEnumerable<MessageTypeMessagingEntityMappingDetails> allMessageTypePathMappings)
        {
            MessagingEntityCreationOptions creationOptions = mappingDetails.CreationOptions;

            if(creationOptions != MessagingEntityCreationOptions.None
                    &&
               !_verifiedExistingMessagingEntities.Contains(mappingDetails))
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
                        create = () =>
                        {
                            MessageTypeMessagingEntityMappingDetails topicMessageTypePathMapping = allMessageTypePathMappings.FirstOrDefault(mtpmd => mtpmd.MessagingEntityType == MessagingEntityType.Topic && mtpmd.Path == topicPath);

                            if(topicMessageTypePathMapping == null)
                            {
                                topicMessageTypePathMapping = new MessageTypeMessagingEntityMappingDetails(mappingDetails.MessageType, topicPath, MessagingEntityType.Topic, MessagingEntityCreationOptions.VerifyAlreadyExists);
                            }

                            EnsureMessagingEntityExists(topicMessageTypePathMapping, allMessageTypePathMappings);

                            _namespaceManager.CreateSubscription(topicPath, subscriptionName);
                        };
                        delete = () => _namespaceManager.DeleteSubscription(topicPath, subscriptionName);

                        break;

                    default:
                        throw new NotSupportedException(string.Format("Unsupported messaging entity type, {0}, requested for creation (path {1}).", mappingDetails.MessagingEntityType, mappingDetails.Path));
                }

                bool alreadyExists = exists();

                if(alreadyExists)
                {
                    if((creationOptions & MessagingEntityCreationOptions.CreateAsTemporary) != 0)
                    {
                        if((creationOptions & MessagingEntityCreationOptions.RecreateExistingTemporary) == 0)
                        {
                            throw new MessagingEntityAlreadyExistsException(mappingDetails.Path, mappingDetails.MessagingEntityType);
                        }

                        try
                        {
                            delete();

                            alreadyExists = false;
                        }
                        catch(UnauthorizedAccessException exception)
                        {
                            throw new UnauthorizedAccessException(string.Format("Unable to delete temporary messaging that already exists at path \"{0}\" due to insufficient access. Make sure the policy being used has 'Manage' permission for the namespace.", mappingDetails.Path), exception);
                        }
                    }
                }

                if(!alreadyExists)
                {
                    if((creationOptions & (MessagingEntityCreationOptions.CreateIfDoesntExist|MessagingEntityCreationOptions.CreateAsTemporary)) == 0)
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
                }

                _verifiedExistingMessagingEntities.Add(mappingDetails);
            }

        }
    }
}
