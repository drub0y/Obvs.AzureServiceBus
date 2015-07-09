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
    public class AzureServiceBusEndpointProvider<TServiceMessage, TMessage, TCommand, TEvent, TRequest, TResponse> : ServiceEndpointProviderBase<TMessage, TCommand, TEvent, TRequest, TResponse>
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
        private readonly MessagePropertyProviderManager<TMessage> _messagePropertyProviderManager;

        public AzureServiceBusEndpointProvider(string serviceName, INamespaceManager namespaceManager, IMessagingFactory messagingFactory, IMessageSerializer serializer, IMessageDeserializerFactory deserializerFactory, List<MessageTypePathMappingDetails> messageTypePathMappings, Func<Assembly, bool> assemblyFilter, Func<Type, bool> typeFilter, MessagePropertyProviderManager<TMessage> messagePropertyProviderManager)
            : base(serviceName)
        {
            _serializer = serializer;
            _deserializerFactory = deserializerFactory;
            _assemblyFilter = assemblyFilter;
            _typeFilter = typeFilter;
            _messageClientEntityFactory = new MessageClientEntityFactory(namespaceManager, messagingFactory, messageTypePathMappings);
            _messagePropertyProviderManager = messagePropertyProviderManager;
        }

        public override IServiceEndpoint<TMessage, TCommand, TEvent, TRequest, TResponse> CreateEndpoint()
        {
            return new ServiceEndpoint<TMessage, TCommand, TEvent, TRequest, TResponse>(
               new MessageSource<TRequest>(_messageClientEntityFactory.CreateMessageReceiver<TRequest>(), _deserializerFactory.Create<TRequest, TServiceMessage>(_assemblyFilter, _typeFilter)),
               new MessageSource<TCommand>(_messageClientEntityFactory.CreateMessageReceiver<TCommand>(), _deserializerFactory.Create<TCommand, TServiceMessage>(_assemblyFilter, _typeFilter)),
               new MessagePublisher<TEvent>(_messageClientEntityFactory.CreateMessageSender<TEvent>(), _serializer, _messagePropertyProviderManager.GetMessagePropertyProviderFor<TEvent>()),
               new MessagePublisher<TResponse>(_messageClientEntityFactory.CreateMessageSender<TResponse>(), _serializer, _messagePropertyProviderManager.GetMessagePropertyProviderFor<TResponse>()),
               typeof(TServiceMessage));
        }


        public override IServiceEndpointClient<TMessage, TCommand, TEvent, TRequest, TResponse> CreateEndpointClient()
        {
            return new ServiceEndpointClient<TMessage, TCommand, TEvent, TRequest, TResponse>(
               new MessageSource<TEvent>(_messageClientEntityFactory.CreateMessageReceiver<TEvent>(), _deserializerFactory.Create<TEvent, TServiceMessage>(_assemblyFilter, _typeFilter)),
               new MessageSource<TResponse>(_messageClientEntityFactory.CreateMessageReceiver<TResponse>(), _deserializerFactory.Create<TResponse, TServiceMessage>(_assemblyFilter, _typeFilter)),
               new MessagePublisher<TRequest>(_messageClientEntityFactory.CreateMessageSender<TRequest>(), _serializer, _messagePropertyProviderManager.GetMessagePropertyProviderFor<TRequest>()),
               new MessagePublisher<TCommand>(_messageClientEntityFactory.CreateMessageSender<TCommand>(), _serializer, _messagePropertyProviderManager.GetMessagePropertyProviderFor<TCommand>()),
               typeof(TServiceMessage));
        }
    }    
}
