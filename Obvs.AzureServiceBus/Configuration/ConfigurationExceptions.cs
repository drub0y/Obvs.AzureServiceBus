using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Obvs.AzureServiceBus.Configuration
{
    [Serializable]
    public class MappingAlreadyExistsForMessageTypeException : Exception
    {
        public MappingAlreadyExistsForMessageTypeException(Type messageType, MessagingEntityType entityType)
            : base(string.Format("A mapping already exists for message type {0} for entity type {1}", messageType.Name, entityType))
        {
            MessageType = messageType;
            EntityType = entityType;
        }

        protected MappingAlreadyExistsForMessageTypeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public Type MessageType
        {
            get;
            private set;
        }

        public MessagingEntityType EntityType
        {
            get;
            private set;
        }
    }

    [Serializable]
    public class AmbiguosMessageTypeMappingException : Exception
    {
        public AmbiguosMessageTypeMappingException(Type messageType, IEnumerable<MessagingEntityType> expectedEntityTypes)
            : base(string.Format("More than one mapping exists for message type {0} for expected entity types {1}", messageType.Name, string.Join(", ", expectedEntityTypes)))
        {
            MessageType = messageType;
            ExpectedEntityTypes = expectedEntityTypes;
        }

        protected AmbiguosMessageTypeMappingException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public Type MessageType
        {
            get;
            private set;
        }

        public IEnumerable<MessagingEntityType> ExpectedEntityTypes
        {
            get;
            private set;
        }
    }

    [Serializable]
    public class MessagingEntityDoesNotAlreadyExistException : Exception
    {
        public MessagingEntityDoesNotAlreadyExistException(string path, MessagingEntityType messagingEntityType)
            : base(string.Format("A messaging entity with a path of \"{0}\" of type {1} does not exist and was not configured to be created automatically.", path, messagingEntityType))
        {
            Path = path;
            MessagingEntityType = messagingEntityType;
        }

        protected MessagingEntityDoesNotAlreadyExistException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
        public string Path
        {
            get;
            private set;
        }

        public MessagingEntityType MessagingEntityType
        {
            get;
            private set;
        }
    }

    [Serializable]
    public class MessagingEntityAlreadyExistsException : Exception
    {
        public MessagingEntityAlreadyExistsException(string path, MessagingEntityType messagingEntityType)
            : base(string.Format("A messaging entity with a path of \"{0}\" of type {1} already exists. To ensure intent and keep your data safe the framwork will not recreate it as temporary unless explicitly configured to do so. You can change the configuration to explicitly enable deletion of existing temporary entities or manually delete the entity.", path, messagingEntityType))
        {
            Path = path;
            MessagingEntityType = messagingEntityType;
        }

        protected MessagingEntityAlreadyExistsException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
        public string Path
        {
            get;
            private set;
        }

        public MessagingEntityType MessagingEntityType
        {
            get;
            private set;
        }
    }
}
