using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.ServiceBus.Messaging;
using Obvs.Configuration;
using Obvs.Types;

namespace Obvs.AzureServiceBus.Configuration
{
    public class AzureServiceBusQueueEndpointProvider<TServiceMessage> : ServiceEndpointProviderBase where TServiceMessage : IMessage
    {
        private readonly Uri _connectionUri;
        private readonly IMessageSerializer _serializer;
        private readonly IMessageDeserializerFactory _deserializerFactory;
        private readonly List<MessageTypePathMappingDetails> _messageTypePathMappings;
        private readonly string _assemblyNameContains;
        private readonly MessagingFactorySettings _settings;

        public AzureServiceBusQueueEndpointProvider(string serviceName, Uri connectionString, IMessageSerializer serializer, IMessageDeserializerFactory deserializerFactory, List<MessageTypePathMappingDetails> messageTypePathMappings, MessagingFactorySettings settings)
            : base(serviceName)
        {
            _connectionUri = connectionString;
            _serializer = serializer;
            _deserializerFactory = deserializerFactory;
            _messageTypePathMappings = messageTypePathMappings;
            _settings = settings;
        }

        public AzureServiceBusQueueEndpointProvider(string serviceName, Uri connectionString, IMessageSerializer serializer, IMessageDeserializerFactory deserializerFactory)
            : this(serviceName, connectionString, serializer, deserializerFactory, new List<MessageTypePathMappingDetails>(), new MessagingFactorySettings())
        {
        }

        public List<MessageTypePathMappingDetails> MessageTypePathMappings
        {
            get
            {
                return _messageTypePathMappings;
            }
        }


        public MessagingFactorySettings Settings
        {
            get
            {
                return _settings;
            }
        }

        public override IServiceEndpoint CreateEndpoint()
        {
            MessageClientEntityFactory factory = new MessageClientEntityFactory(_connectionUri, _settings ?? new MessagingFactorySettings(), _messageTypePathMappings);

            return new ServiceEndpoint(
               new MessageSource<IRequest>(factory.CreateMessageReceiver<IRequest>(), _deserializerFactory.Create<IRequest, TServiceMessage>(_assemblyNameContains)),
               new MessageSource<ICommand>(factory.CreateMessageReceiver<ICommand>(), _deserializerFactory.Create<ICommand, TServiceMessage>(_assemblyNameContains)),
               new MessagePublisher<IEvent>(factory.CreateMessageSender<IEvent>(), _serializer, new DefaultPropertyProvider<IEvent>()),
               new MessagePublisher<IResponse>(factory.CreateMessageSender<IResponse>(), _serializer, new DefaultPropertyProvider<IResponse>()),
               typeof(TServiceMessage));
        }


        public override IServiceEndpointClient CreateEndpointClient()
        {
            MessageClientEntityFactory factory = new MessageClientEntityFactory(_connectionUri, _settings ?? new MessagingFactorySettings(), _messageTypePathMappings);

            return new ServiceEndpointClient(
               new MessageSource<IEvent>(factory.CreateMessageReceiver<IEvent>(), _deserializerFactory.Create<IEvent, TServiceMessage>(_assemblyNameContains)),
               new MessageSource<IResponse>(factory.CreateMessageReceiver<IResponse>(), _deserializerFactory.Create<IResponse, TServiceMessage>(_assemblyNameContains)),
               new MessagePublisher<IRequest>(factory.CreateMessageSender<IRequest>(), _serializer, new DefaultPropertyProvider<IRequest>()),
               new MessagePublisher<ICommand>(factory.CreateMessageSender<ICommand>(), _serializer, new DefaultPropertyProvider<ICommand>()),
               typeof(TServiceMessage));
        }
    }    
}
