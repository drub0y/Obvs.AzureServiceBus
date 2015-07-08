using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
    public class AzureServiceBusQueueEndpointProvider<TServiceMessage, TMessage, TCommand, TEvent, TRequest, TResponse> : ServiceEndpointProviderBase<TMessage, TCommand, TEvent, TRequest, TResponse>
        where TMessage : class
        where TCommand : class, TMessage
        where TEvent : class, TMessage
        where TRequest : class, TMessage
        where TResponse : class, TMessage
        where TServiceMessage : class
    {
        private readonly IMessageSerializer _serializer;
        private readonly IMessageDeserializerFactory _deserializerFactory;
        private readonly Func<Assembly, bool> _assemblyFilter;
        private readonly Func<Type, bool> _typeFilter;
        private readonly MessageClientEntityFactory _messageClientEntityFactory;
        private readonly IBrokeredMessageRequestResponseCorrelationMapper _requestResponseCorrelationProvider;

        public AzureServiceBusQueueEndpointProvider(string serviceName, INamespaceManager namespaceManager, IMessagingFactory messagingFactory, IMessageSerializer serializer, IMessageDeserializerFactory deserializerFactory, List<MessageTypePathMappingDetails> messageTypePathMappings, Func<Assembly, bool> assemblyFilter, Func<Type, bool> typeFilter, IBrokeredMessageRequestResponseCorrelationMapper requestResponseCorrelationProvider)
            : base(serviceName)
        {
            _serializer = serializer;
            _deserializerFactory = deserializerFactory;
            _assemblyFilter = assemblyFilter;
            _typeFilter = typeFilter;
            _messageClientEntityFactory = new MessageClientEntityFactory(namespaceManager, messagingFactory, messageTypePathMappings);
            _requestResponseCorrelationProvider = requestResponseCorrelationProvider;
        }

        public override IServiceEndpoint<TMessage, TCommand, TEvent, TRequest, TResponse> CreateEndpoint()
        {
            return new ServiceEndpoint<TMessage, TCommand, TEvent, TRequest, TResponse>(
               new MessageSource<TRequest>(_messageClientEntityFactory.CreateMessageReceiver<TRequest>(), _deserializerFactory.Create<TRequest, TServiceMessage>(_assemblyFilter, _typeFilter)),
               new MessageSource<TCommand>(_messageClientEntityFactory.CreateMessageReceiver<TCommand>(), _deserializerFactory.Create<TCommand, TServiceMessage>(_assemblyFilter, _typeFilter)),
               new MessagePublisher<TEvent>(_messageClientEntityFactory.CreateMessageSender<TEvent>(), _serializer, new DefaultPropertyProvider<TEvent>(), _requestResponseCorrelationProvider),
               new MessagePublisher<TResponse>(_messageClientEntityFactory.CreateMessageSender<TResponse>(), _serializer, new DefaultPropertyProvider<TResponse>(), _requestResponseCorrelationProvider),
               typeof(TServiceMessage));
        }


        public override IServiceEndpointClient<TMessage, TCommand, TEvent, TRequest, TResponse> CreateEndpointClient()
        {
            return new ServiceEndpointClient<TMessage, TCommand, TEvent, TRequest, TResponse>(
               new MessageSource<TEvent>(_messageClientEntityFactory.CreateMessageReceiver<TEvent>(), _deserializerFactory.Create<TEvent, TServiceMessage>(_assemblyFilter, _typeFilter)),
               new MessageSource<TResponse>(_messageClientEntityFactory.CreateMessageReceiver<TResponse>(), _deserializerFactory.Create<TResponse, TServiceMessage>(_assemblyFilter, _typeFilter)),
               new MessagePublisher<TRequest>(_messageClientEntityFactory.CreateMessageSender<TRequest>(), _serializer, new DefaultPropertyProvider<TRequest>(), _requestResponseCorrelationProvider),
               new MessagePublisher<TCommand>(_messageClientEntityFactory.CreateMessageSender<TCommand>(), _serializer, new DefaultPropertyProvider<TCommand>(), _requestResponseCorrelationProvider),
               typeof(TServiceMessage));
        }
    }    
}
