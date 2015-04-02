using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ServiceBus.Messaging;
using Obvs.AzureServiceBus.Infrastructure;
using Obvs.Types;

namespace Obvs.AzureServiceBus
{
    public class MessagePublisher<TMessage> : IMessagePublisher<TMessage>
        where TMessage : IMessage
    {
        private IMessageSender _messageSender;
        private IMessageSerializer _serializer;
        private IMessagePropertyProvider<TMessage> _propertyProvider;

        public MessagePublisher(MessageSender messageSender, IMessageSerializer serializer, IMessagePropertyProvider<TMessage> propertyProvider) : this(new MessageSenderWrapper(messageSender), serializer, propertyProvider)
        {
        }

        internal MessagePublisher(IMessageSender messageSender, IMessageSerializer serializer, IMessagePropertyProvider<TMessage> propertyProvider)
        {
            if(messageSender == null) throw new ArgumentNullException("messageSender");
            if(serializer == null) throw new ArgumentNullException("serializer");
            if(propertyProvider == null) throw new ArgumentNullException("propertyProvider");
            
            _messageSender = messageSender;
            _serializer = serializer;
            _propertyProvider = propertyProvider;
        }


        public void Publish(TMessage message)
        {
            List<KeyValuePair<string, object>> properties = _propertyProvider.GetProperties(message).ToList();

            Publish(message, properties);
        }

        public void Dispose()
        {
        }

        private void Publish(TMessage message, List<KeyValuePair<string, object>> properties)
        {
            properties.Add(new KeyValuePair<string, object>(MessagePropertyNames.TypeName, message.GetType().Name));

            object data = _serializer.Serialize(message);

            BrokeredMessage brokeredMessage = new BrokeredMessage(data);

            SetCorrelationIdentifierIfApplicable(message, brokeredMessage);

            foreach(KeyValuePair<string, object> property in properties)
            {
                brokeredMessage.Properties.Add(property);
            }

            _messageSender.Send(brokeredMessage);
        }

        private void SetCorrelationIdentifierIfApplicable(TMessage message,BrokeredMessage brokeredMessage)
        {
            IRequest requestMessage = message as IRequest;

            if(requestMessage != null)
            {
                brokeredMessage.CorrelationId = requestMessage.RequestId;
            }
            else
            {
                IResponse responseMessage = message as IResponse;

                if(responseMessage != null)
                {
                    brokeredMessage.CorrelationId = responseMessage.RequestId;
                }
            }
        }
    }
}
