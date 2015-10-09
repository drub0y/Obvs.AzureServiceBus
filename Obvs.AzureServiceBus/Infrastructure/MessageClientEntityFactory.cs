using System;
using System.Collections.Generic;
using System.Linq;
using Obvs.AzureServiceBus.Configuration;
using Obvs.AzureServiceBus.Infrastructure;

namespace Obvs.AzureServiceBus
{
    internal sealed class MessageClientEntityFactory : IMessageClientEntityFactory
    {
        private readonly IMessagingFactory _messagingFactory;
        private readonly List<MessageTypeMessagingEntityMappingDetails> _messageTypePathMappings;

        public MessageClientEntityFactory(IMessagingFactory messagingFactory, List<MessageTypeMessagingEntityMappingDetails> messageTypePathMappings)
        {
            if(messagingFactory == null) throw new ArgumentNullException("messagingFactory");
            if(messageTypePathMappings == null) throw new ArgumentNullException("messageTypePathMappings");
            if(messageTypePathMappings.Count == 0) throw new ArgumentException("No message types have been mapped.", "messageTypePathMappings");

            _messagingFactory = messagingFactory;
            _messageTypePathMappings = messageTypePathMappings;
        }

        public IMessageReceiver CreateMessageReceiver(Type messageType)
        {
            MessageTypeMessagingEntityMappingDetails mappingDetails = GetMappingDetails(messageType, MessagingEntityType.Queue, MessagingEntityType.Subscription);

            IMessageReceiver messageReceiver;

            if(mappingDetails != null)
            {
                messageReceiver = _messagingFactory.CreateMessageReceiver(messageType, mappingDetails.Path, mappingDetails.ReceiveMode);
            }
            else
            {
                // TODO: log

                messageReceiver = new UnconfiguredMessageReceiver(messageType);
            }

            return messageReceiver;
        }

        public IMessageSender CreateMessageSender(Type messageType)
        {
            MessageTypeMessagingEntityMappingDetails mappingDetails = GetMappingDetails(messageType, MessagingEntityType.Queue, MessagingEntityType.Topic);

            IMessageSender messageSender;
            
            if(mappingDetails != null)
            {
                messageSender = _messagingFactory.CreateMessageSender(messageType, mappingDetails.Path);
            }
            else
            {
                // TODO: log
                messageSender = new UnconfiguredMessageSender(messageType);
            }

            return messageSender;
        }

        private MessageTypeMessagingEntityMappingDetails GetMappingDetails(Type messageType, params MessagingEntityType[] expectedEntityTypes)
        {
            // Start by reducing the set to only those that match the messaging entity types we're looking for
            IEnumerable<MessageTypeMessagingEntityMappingDetails> messageTypePathMappingDetailsForMessagingEntityTypes = (from mtpm in _messageTypePathMappings
                                                                                                               join eet in expectedEntityTypes on mtpm.MessagingEntityType equals eet
                                                                                                               select mtpm);


            MessageTypeMessagingEntityMappingDetails resolvedMessageTypePathMappingDetails = null;

            foreach(MessageTypeMessagingEntityMappingDetails messageTypePathMappingDetails in messageTypePathMappingDetailsForMessagingEntityTypes)
            {
                Type supportedMessageType = messageTypePathMappingDetails.MessageType;

                // If the type matches exactly, then that's the one we use and we can stop looking for anything else
                if(supportedMessageType == messageType)
                {
                    resolvedMessageTypePathMappingDetails = messageTypePathMappingDetails;

                    break;
                }
                else
                {
                    // Check if the type of this mapping is assignable from the message type in question
                    if(supportedMessageType.IsAssignableFrom(messageType))
                    {
                        // If we already found a mapping which might have worked and now we found another, then
                        // we must fail and report the ambiguity
                        if(resolvedMessageTypePathMappingDetails != null)
                        {
                            throw new AmbiguosMessageTypeMappingException(messageType, expectedEntityTypes);
                        }

                        // Remember this mapping, but we will continue looking to make sure it is the only one
                        // that works for the specified message type (could be more than one)
                        resolvedMessageTypePathMappingDetails = messageTypePathMappingDetails;
                    }
                }
            }

            return resolvedMessageTypePathMappingDetails;
        }
    }
}
