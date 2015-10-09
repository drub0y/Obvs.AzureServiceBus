using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Obvs.AzureServiceBus.Configuration
{
    public enum MessagingEntityType
    {
        Queue = 0,
        Topic = 1,
        Subscription = 2
    }

    [Flags]
    public enum MessagingEntityCreationOptions
    {
        None = 0,
        VerifyAlreadyExists = 1,
        CreateIfDoesntExist = 2,
        CreateAsTemporary = 4,
        RecreateExistingTemporary = 8
    }

    public class MessageTypeMessagingEntityMappingDetails
    {
        private readonly Type _messageType;
        private readonly string _path;
        private readonly MessagingEntityType _messagingEntityType;
        private readonly MessagingEntityCreationOptions _creationOptions;
        private readonly MessageReceiveMode _receiveMode;

        public MessageTypeMessagingEntityMappingDetails(Type messageType, string path, MessagingEntityType messagingEntityType)
            : this(messageType, path, messagingEntityType, MessagingEntityCreationOptions.None)
        {
        }

        public MessageTypeMessagingEntityMappingDetails(Type messageType, string path, MessagingEntityType messagingEntityType, MessagingEntityCreationOptions creationOptions)
            : this(messageType, path, messagingEntityType, creationOptions, MessageReceiveMode.ReceiveAndDelete)
        {
        }

        public MessageTypeMessagingEntityMappingDetails(Type messageType, string path, MessagingEntityType messagingEntityType, MessagingEntityCreationOptions creationOptions, MessageReceiveMode receiveMode)
        {
            _messageType = messageType;
            _path = path;
            _messagingEntityType = messagingEntityType;
            _creationOptions = creationOptions;
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

        public MessagingEntityType MessagingEntityType
        {
            get
            {
                return _messagingEntityType;
            }
        }

        public MessagingEntityCreationOptions CreationOptions
        {
            get
            {
                return _creationOptions;
            }
        }

        public MessageReceiveMode ReceiveMode
        {
            get
            {
                return _receiveMode;
            }
        }

        public override int GetHashCode()
        {
            return _receiveMode.GetHashCode() ^ _creationOptions.GetHashCode() ^ _messageType.GetHashCode() ^ _messagingEntityType.GetHashCode() ^ _path.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            MessageTypeMessagingEntityMappingDetails compareTo = obj as MessageTypeMessagingEntityMappingDetails;

            return compareTo != null
                        &&
                    (!Object.ReferenceEquals(this, compareTo)
                        ||
                    (_receiveMode == compareTo._receiveMode
                                &&
                             _creationOptions == compareTo._creationOptions
                                &&
                             _path == compareTo._path
                                &&
                             _messageType == compareTo._messageType
                                &&
                             _messagingEntityType == compareTo._messagingEntityType));
        }

        public sealed class MessagingEntityTypeAndPathComparer : IEqualityComparer<MessageTypeMessagingEntityMappingDetails>
        {
            public bool Equals(MessageTypeMessagingEntityMappingDetails x, MessageTypeMessagingEntityMappingDetails y)
            {
                return x.MessagingEntityType == y.MessagingEntityType
                            &&
                       StringComparer.OrdinalIgnoreCase.Equals(x, y);
            }

            public int GetHashCode(MessageTypeMessagingEntityMappingDetails obj)
            {
                return obj.MessagingEntityType.GetHashCode() ^ obj.Path.GetHashCode();
            }
        }
    }
}
