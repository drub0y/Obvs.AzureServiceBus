using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Obvs.AzureServiceBus.Configuration;
using Obvs.AzureServiceBus.Infrastructure;
using Obvs.MessageProperties;
using Obvs.Serialization;
using Obvs.Types;

namespace Obvs.AzureServiceBus
{
    public class MessagePublisher<TMessage> : IMessagePublisher<TMessage>
        where TMessage : class
    {
        private readonly IMessagingEntityFactory _messagingEntityFactory;
        private readonly IMessageSerializer _serializer;
        private readonly IMessagePropertyProvider<TMessage> _propertyProvider;
        private readonly ConcurrentDictionary<Type, IMessageSender> _messageTypeMessageSenderMap;

        internal MessagePublisher(IMessagingEntityFactory messagingEntityFactory, IMessageSerializer serializer, IMessagePropertyProvider<TMessage> propertyProvider)
        {
            if(messagingEntityFactory == null) throw new ArgumentNullException("messagingEntityFactory");
            if(serializer == null) throw new ArgumentNullException("serializer");
            if(propertyProvider == null) throw new ArgumentNullException("propertyProvider");

            _messagingEntityFactory = messagingEntityFactory;
            _serializer = serializer;
            _propertyProvider = propertyProvider;
            _messageTypeMessageSenderMap = new ConcurrentDictionary<Type, IMessageSender>();
        }


        public Task PublishAsync(TMessage message)
        {
            IEnumerable<KeyValuePair<string, object>> properties = _propertyProvider.GetProperties(message);

            return PublishAsync(message, properties);
        }

        public void Dispose()
        {
            foreach(IMessageSender messageSender in _messageTypeMessageSenderMap.Values)
            {
                messageSender.Dispose();
            }
        }

        private async Task PublishAsync(TMessage message, IEnumerable<KeyValuePair<string, object>> properties)
        {
            using(MemoryStream messageBodyStream = new MemoryStream())
            {
                _serializer.Serialize(messageBodyStream, message);

                messageBodyStream.Position = 0;

                BrokeredMessage brokeredMessage = new BrokeredMessage(messageBodyStream);

                SetSessionAndCorrelationIdentifiersIfApplicable(message, brokeredMessage);
            
                SetProperties(message, properties, brokeredMessage);

                IMessageSender messageSenderForMessageType = GetMessageSenderForMessageType(message);

                await messageSenderForMessageType.SendAsync(brokeredMessage);
            }
        }        

        private static void SetProperties(TMessage message, IEnumerable<KeyValuePair<string, object>> properties, BrokeredMessage brokeredMessage)
        {
            brokeredMessage.Properties.Add(MessagePropertyNames.TypeName, message.GetType().Name);

                foreach(KeyValuePair<string, object> property in properties)
                {
                    brokeredMessage.Properties.Add(property);
                }
        }

        private void SetSessionAndCorrelationIdentifiersIfApplicable(TMessage message, BrokeredMessage brokeredMessage)
        {
            IRequest requestMessage = message as IRequest;

            if(requestMessage != null)
            {
                SetRequestSessionAndCorrelationIdentifiers(brokeredMessage, requestMessage);
            }
            else
            {
                IResponse responseMessage = message as IResponse;

                if(responseMessage != null)
                {
                    SetResponseSessionAndCorrelationIdentifiers(brokeredMessage, responseMessage);
                }                
            }
        }

        private static void SetRequestSessionAndCorrelationIdentifiers(BrokeredMessage brokeredMessage, IRequest requestMessage)
        {
            string requesterId = requestMessage.RequesterId;

            if(!string.IsNullOrEmpty(requesterId))
            {
                brokeredMessage.ReplyToSessionId = requesterId;
            }

            brokeredMessage.CorrelationId = requestMessage.RequestId;
        }

        private static void SetResponseSessionAndCorrelationIdentifiers(BrokeredMessage brokeredMessage, IResponse responseMessage)
        {
            string requesterId = responseMessage.RequesterId;

            if(!string.IsNullOrEmpty(requesterId))
            {
                brokeredMessage.SessionId = requesterId;
            }

            brokeredMessage.CorrelationId = responseMessage.RequestId;
        }

        private IMessageSender GetMessageSenderForMessageType(TMessage message)
        {
            Type messageType = message.GetType();

            return _messageTypeMessageSenderMap.GetOrAdd(
                messageType,
                CreateMessageSenderForMessageType);
        }

        private IMessageSender CreateMessageSenderForMessageType(Type messageType)
        {
            return _messagingEntityFactory.CreateMessageSender(messageType);
        }

    }
}
