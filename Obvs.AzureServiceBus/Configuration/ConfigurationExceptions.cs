using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Obvs.AzureServiceBus.Configuration
{
    [Serializable]
    public class MoreThanOneMappingExistsForMessageTypeException : Exception
    {
        public MoreThanOneMappingExistsForMessageTypeException(Type messageType, IEnumerable<MessagingEntityType> expectedEntityTypes, Exception innerException)
            : base(string.Format("More than one mapping exists for message type {0} for expected entity types {1}", messageType.Name, string.Join(", ", expectedEntityTypes)), innerException)
        {
            MessageType = messageType;
            ExpectedEntityTypes = expectedEntityTypes;
        }

        protected MoreThanOneMappingExistsForMessageTypeException(SerializationInfo info, StreamingContext context)
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
}
