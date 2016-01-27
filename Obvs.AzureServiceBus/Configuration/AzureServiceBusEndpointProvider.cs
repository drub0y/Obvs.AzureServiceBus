using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Obvs.AzureServiceBus.Infrastructure;
using Obvs.Configuration;
using Obvs.MessageProperties;
using Obvs.Serialization;

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
        private readonly IMessageSerializer _messageSerializer;
        private readonly IMessageDeserializerFactory _messageDeserializerFactory;
        private readonly Func<Assembly, bool> _assemblyFilter;
        private readonly Func<Type, bool> _typeFilter;
        private readonly List<MessageTypeMessagingEntityMappingDetails> _messageTypePathMappings;
        private readonly MessagingEntityFactory _messageClientEntityFactory;
        private readonly MessagePropertyProviderManager<TMessage> _messagePropertyProviderManager;

        public AzureServiceBusEndpointProvider(string serviceName, IMessagingFactory messagingFactory, IMessageSerializer messageSerializer, IMessageDeserializerFactory messageDeserializerFactory, List<MessageTypeMessagingEntityMappingDetails> messageTypePathMappings, Func<Assembly, bool> assemblyFilter, Func<Type, bool> typeFilter, MessagePropertyProviderManager<TMessage> messagePropertyProviderManager)
            : base(serviceName)
        {
            if(messagingFactory == null) throw new ArgumentNullException(nameof(messagingFactory));
            if(messageSerializer == null) throw new ArgumentNullException(nameof(messageSerializer));
            if(messageDeserializerFactory == null) throw new ArgumentNullException(nameof(messageDeserializerFactory));
            if(messageTypePathMappings == null) throw new ArgumentNullException(nameof(messageTypePathMappings));
            if(messageTypePathMappings.Count == 0) throw new ArgumentException("An empty set of path mappings was specified.", nameof(messageTypePathMappings));
            if(messagePropertyProviderManager == null) throw new ArgumentNullException(nameof(messagePropertyProviderManager));

            _messageSerializer = messageSerializer;
            _messageDeserializerFactory = messageDeserializerFactory;
            _assemblyFilter = assemblyFilter;
            _typeFilter = typeFilter;
            _messageTypePathMappings = messageTypePathMappings;
            _messagePropertyProviderManager = messagePropertyProviderManager;

            _messageClientEntityFactory = new MessagingEntityFactory(messagingFactory, messageTypePathMappings);
        }

        public override IServiceEndpoint<TMessage, TCommand, TEvent, TRequest, TResponse> CreateEndpoint()
        {
            return new ServiceEndpoint<TMessage, TCommand, TEvent, TRequest, TResponse>(
               GetMessageSource<TRequest>(),
               GetMessageSource<TCommand>(),
               new MessagePublisher<TEvent>(_messageClientEntityFactory, _messageSerializer, _messagePropertyProviderManager.GetMessagePropertyProviderFor<TEvent>()),
               new MessagePublisher<TResponse>(_messageClientEntityFactory, _messageSerializer, _messagePropertyProviderManager.GetMessagePropertyProviderFor<TResponse>()),
               typeof(TServiceMessage));
        }


        public override IServiceEndpointClient<TMessage, TCommand, TEvent, TRequest, TResponse> CreateEndpointClient()
        {

            return new ServiceEndpointClient<TMessage, TCommand, TEvent, TRequest, TResponse>(
               GetMessageSource<TEvent>(),
               GetMessageSource<TResponse>(),
               new MessagePublisher<TRequest>(_messageClientEntityFactory, _messageSerializer, _messagePropertyProviderManager.GetMessagePropertyProviderFor<TRequest>()),
               new MessagePublisher<TCommand>(_messageClientEntityFactory, _messageSerializer, _messagePropertyProviderManager.GetMessagePropertyProviderFor<TCommand>()),
               typeof(TServiceMessage));
        }

        private IMessageSource<TSourceMessage> GetMessageSource<TSourceMessage>() where TSourceMessage : class, TMessage
        {
            // Find mappings for source types thare are assignable from the target type
            var sourceMessageTypePathMappings = (from mtpm in _messageTypePathMappings
                                                 where (mtpm.MessagingEntityType == MessagingEntityType.Queue
                                                           ||
                                                       mtpm.MessagingEntityType == MessagingEntityType.Subscription)
                                                           &&
                                                       typeof(TSourceMessage).IsAssignableFrom(mtpm.MessageType)
                                                 select mtpm);

            IMessageSource<TSourceMessage> result;

            // If there's only one target path mapping for this message type then just return a single MessageSource<T> instance (avoid overhead of MergedMessageSource)
            if(sourceMessageTypePathMappings.Count() == 1)
            {
                result = CreateMessageSource<TSourceMessage>(sourceMessageTypePathMappings.First().MessageType);
            }
            else
            {
                result = new MergedMessageSource<TSourceMessage>(sourceMessageTypePathMappings.Select(mtpm => CreateMessageSource<TSourceMessage>(mtpm.MessageType)));
            }

            return result;
        }

        private IMessageSource<TSourceMessage> CreateMessageSource<TSourceMessage>(Type messageType) where TSourceMessage : class, TMessage
        {
            Type messageSourceType = typeof(MessageSource<>).MakeGenericType(messageType);
            Type messageSourceDeserializerType = typeof(IMessageDeserializer<>).MakeGenericType(messageType);

            return Expression.Lambda<Func<IMessageSource<TSourceMessage>>>(
                    Expression.New(messageSourceType.GetConstructor(new Type[] { typeof(IMessagingEntityFactory), typeof(IEnumerable<>).MakeGenericType(messageSourceDeserializerType) }),
                Expression.Constant(_messageClientEntityFactory),
                Expression.Call(
                    Expression.Constant(_messageDeserializerFactory),
                    typeof(IMessageDeserializerFactory).GetMethod("Create").MakeGenericMethod(messageType, typeof(TServiceMessage)),
                    Expression.Constant(_assemblyFilter, typeof(Func<Assembly, bool>)),
                    Expression.Constant(_typeFilter, typeof(Func<Type, bool>))))).Compile().Invoke();
        }
    }
}
