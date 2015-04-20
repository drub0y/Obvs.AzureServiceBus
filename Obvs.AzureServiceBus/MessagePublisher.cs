using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Obvs.AzureServiceBus.Infrastructure;
using Obvs.MessageProperties;
using Obvs.Serialization;
using Obvs.Types;

namespace Obvs.AzureServiceBus
{
    public class MessagePublisher<TMessage> : IMessagePublisher<TMessage>
        where TMessage : IMessage
    {
        private IMessageSender _messageSender;
        private IMessageSerializer _serializer;
        private IMessagePropertyProvider<TMessage> _propertyProvider;
        private IBrokeredMessageRequestResponseCorrelationMapper _requestCorrelationProvider;

        public MessagePublisher(MessageSender messageSender, IMessageSerializer serializer, IMessagePropertyProvider<TMessage> propertyProvider, IBrokeredMessageRequestResponseCorrelationMapper requestCorrelationProvider)
            : this(new MessageSenderWrapper(messageSender), serializer, propertyProvider, requestCorrelationProvider)
        {
        }

        internal MessagePublisher(IMessageSender messageSender, IMessageSerializer serializer, IMessagePropertyProvider<TMessage> propertyProvider, IBrokeredMessageRequestResponseCorrelationMapper requestCorrelationProvider)
        {
            if(messageSender == null) throw new ArgumentNullException("messageSender");
            if(serializer == null) throw new ArgumentNullException("serializer");
            if(propertyProvider == null) throw new ArgumentNullException("propertyProvider");
            if(requestCorrelationProvider == null) throw new ArgumentNullException("requestCorrelationProvider");
            
            _messageSender = messageSender;
            _serializer = serializer;
            _propertyProvider = propertyProvider;
            _requestCorrelationProvider = requestCorrelationProvider;
        }


        public Task PublishAsync(TMessage message)
        {
            IEnumerable<KeyValuePair<string, object>> properties = _propertyProvider.GetProperties(message);

            return PublishAsync(message, properties);
        }

        public void Dispose()
        {
            _messageSender.Dispose();
        }

        private async Task PublishAsync(TMessage message, IEnumerable<KeyValuePair<string, object>> properties)
        {
            using(MemoryStream messageBodyStream = new MemoryStream())
            {
                _serializer.Serialize(messageBodyStream, message);

                messageBodyStream.Position = 0;

                BrokeredMessage brokeredMessage = new BrokeredMessage(messageBodyStream);

                SetCorrelationIdentifiersIfApplicable(message, brokeredMessage);
            
                SetProperties(message, properties, brokeredMessage);

                await _messageSender.SendAsync(brokeredMessage);
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

        private void SetCorrelationIdentifiersIfApplicable(TMessage message, BrokeredMessage brokeredMessage)
        {
            IRequest requestMessage = message as IRequest;

            if(requestMessage != null)
            {
                _requestCorrelationProvider.MapFromRequest(requestMessage, brokeredMessage);
            }
            else
            {
                IResponse responseMessage = message as IResponse;

                if(responseMessage != null)
                {
                    _requestCorrelationProvider.MapFromResponse(responseMessage, brokeredMessage);
                }
            }
        }
    }
}
