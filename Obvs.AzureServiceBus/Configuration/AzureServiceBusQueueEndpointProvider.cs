using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Obvs.AzureServiceBus.Infrastructure;
using Obvs.Configuration;
using Obvs.MessageProperties;
using Obvs.Serialization;
using Obvs.Types;

namespace Obvs.AzureServiceBus.Configuration
{
    public class AzureServiceBusQueueEndpointProvider<TServiceMessage> : ServiceEndpointProviderBase where TServiceMessage : IMessage
    {
        private readonly IMessageSerializer _serializer;
        private readonly IMessageDeserializerFactory _deserializerFactory;
        private readonly List<MessageTypePathMappingDetails> _messageTypePathMappings;
        private readonly string _assemblyNameContains;
        private readonly INamespaceManager _namespaceManager;
        private readonly IMessagingFactory _messagingFactory;

        public AzureServiceBusQueueEndpointProvider(string serviceName, INamespaceManager namespaceManager, IMessagingFactory messagingFactory, IMessageSerializer serializer, IMessageDeserializerFactory deserializerFactory, List<MessageTypePathMappingDetails> messageTypePathMappings, string assemblyNameContains)
            : base(serviceName)
        {
            _namespaceManager = namespaceManager;
            _messagingFactory = messagingFactory;
            _serializer = serializer;
            _deserializerFactory = deserializerFactory;
            _messageTypePathMappings = messageTypePathMappings;
            _assemblyNameContains = assemblyNameContains;
        }

        public List<MessageTypePathMappingDetails> MessageTypePathMappings
        {
            get
            {
                return _messageTypePathMappings;
            }
        }

        public override IServiceEndpoint CreateEndpoint()
        {
            MessageClientEntityFactory factory = new MessageClientEntityFactory(_namespaceManager, _messagingFactory, _messageTypePathMappings);

            return new ServiceEndpoint(
               new MessageSource<IRequest>(factory.CreateMessageReceiver<IRequest>(), _deserializerFactory.Create<IRequest, TServiceMessage>(_assemblyNameContains)),
               new MessageSource<ICommand>(factory.CreateMessageReceiver<ICommand>(), _deserializerFactory.Create<ICommand, TServiceMessage>(_assemblyNameContains)),
               new MessagePublisher<IEvent>(factory.CreateMessageSender<IEvent>(), _serializer, new DefaultPropertyProvider<IEvent>()),
               new MessagePublisher<IResponse>(factory.CreateMessageSender<IResponse>(), _serializer, new DefaultPropertyProvider<IResponse>()),
               typeof(TServiceMessage));
        }


        public override IServiceEndpointClient CreateEndpointClient()
        {
            MessageClientEntityFactory factory = new MessageClientEntityFactory(_namespaceManager, _messagingFactory, _messageTypePathMappings);

            return new ServiceEndpointClient(
               new MessageSource<IEvent>(factory.CreateMessageReceiver<IEvent>(), _deserializerFactory.Create<IEvent, TServiceMessage>(_assemblyNameContains)),
               new MessageSource<IResponse>(factory.CreateMessageReceiver<IResponse>(), _deserializerFactory.Create<IResponse, TServiceMessage>(_assemblyNameContains)),
               new MessagePublisher<IRequest>(factory.CreateMessageSender<IRequest>(), _serializer, new DefaultPropertyProvider<IRequest>()),
               new MessagePublisher<ICommand>(factory.CreateMessageSender<ICommand>(), _serializer, new DefaultPropertyProvider<ICommand>()),
               typeof(TServiceMessage));
        }
    }    
}
