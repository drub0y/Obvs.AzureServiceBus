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
        private readonly string _assemblyNameContains;
        private readonly MessageClientEntityFactory _messageClientEntityFactory;
        private readonly IBrokeredMessageRequestResponseCorrelationMapper _requestResponseCorrelationProvider;

        public AzureServiceBusQueueEndpointProvider(string serviceName, INamespaceManager namespaceManager, IMessagingFactory messagingFactory, IMessageSerializer serializer, IMessageDeserializerFactory deserializerFactory, List<MessageTypePathMappingDetails> messageTypePathMappings, string assemblyNameContains, IBrokeredMessageRequestResponseCorrelationMapper requestResponseCorrelationProvider)
            : base(serviceName)
        {
            _serializer = serializer;
            _deserializerFactory = deserializerFactory;
            _assemblyNameContains = assemblyNameContains;
            _messageClientEntityFactory = new MessageClientEntityFactory(namespaceManager, messagingFactory, messageTypePathMappings);
            _requestResponseCorrelationProvider = requestResponseCorrelationProvider;
        }

        public override IServiceEndpoint CreateEndpoint()
        {
            return new ServiceEndpoint(
               new MessageSource<IRequest>(_messageClientEntityFactory.CreateMessageReceiver<IRequest>(), _deserializerFactory.Create<IRequest, TServiceMessage>(_assemblyNameContains)),
               new MessageSource<ICommand>(_messageClientEntityFactory.CreateMessageReceiver<ICommand>(), _deserializerFactory.Create<ICommand, TServiceMessage>(_assemblyNameContains)),
               new MessagePublisher<IEvent>(_messageClientEntityFactory.CreateMessageSender<IEvent>(), _serializer, new DefaultPropertyProvider<IEvent>(), _requestResponseCorrelationProvider),
               new MessagePublisher<IResponse>(_messageClientEntityFactory.CreateMessageSender<IResponse>(), _serializer, new DefaultPropertyProvider<IResponse>(), _requestResponseCorrelationProvider),
               typeof(TServiceMessage));
        }


        public override IServiceEndpointClient CreateEndpointClient()
        {
            return new ServiceEndpointClient(
               new MessageSource<IEvent>(_messageClientEntityFactory.CreateMessageReceiver<IEvent>(), _deserializerFactory.Create<IEvent, TServiceMessage>(_assemblyNameContains)),
               new MessageSource<IResponse>(_messageClientEntityFactory.CreateMessageReceiver<IResponse>(), _deserializerFactory.Create<IResponse, TServiceMessage>(_assemblyNameContains)),
               new MessagePublisher<IRequest>(_messageClientEntityFactory.CreateMessageSender<IRequest>(), _serializer, new DefaultPropertyProvider<IRequest>(), _requestResponseCorrelationProvider),
               new MessagePublisher<ICommand>(_messageClientEntityFactory.CreateMessageSender<ICommand>(), _serializer, new DefaultPropertyProvider<ICommand>(), _requestResponseCorrelationProvider),
               typeof(TServiceMessage));
        }
    }    
}
