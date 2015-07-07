using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Obvs.AzureServiceBus.Infrastructure;
using Obvs.Configuration;
using Obvs.Serialization;
using Obvs.Types;

namespace Obvs.AzureServiceBus.Configuration
{
    public enum MessageReceiveMode
    {
        PeekLock,
        ReceiveAndDelete
    }

    public interface ICanAddAzureServiceBusServiceName<TMessage, TCommand, TEvent, TRequest, TResponse>
        where TMessage : class
        where TCommand : class, TMessage
        where TEvent : class, TMessage
        where TRequest : class, TMessage
        where TResponse : class, TMessage
    {
        ICanSpecifyAzureServiceBusNamespace<TMessage, TCommand, TEvent, TRequest, TResponse> Named(string serviceName);
    }

    public interface ICanSpecifyAzureServiceBusNamespace<TMessage, TCommand, TEvent, TRequest, TResponse>
        where TMessage : class
        where TCommand : class, TMessage
        where TEvent : class, TMessage
        where TRequest : class, TMessage
        where TResponse : class, TMessage
    {
        ICanSpecifyAzureServiceBusMessagingFactory<TMessage, TCommand, TEvent, TRequest, TResponse> WithConnectionString(string connectionString);
        ICanSpecifyAzureServiceBusMessagingFactory<TMessage, TCommand, TEvent, TRequest, TResponse> WithNamespaceManager(INamespaceManager namespaceManager);
        ICanSpecifyAzureServiceBusMessagingFactory<TMessage, TCommand, TEvent, TRequest, TResponse> WithNamespaceManager(NamespaceManager namespaceManager);
    }

    public interface ICanSpecifyAzureServiceBusMessagingFactory<TMessage, TCommand, TEvent, TRequest, TResponse> : ICanSpecifyAzureServiceBusMessagingEntity<TMessage, TCommand, TEvent, TRequest, TResponse>
        where TMessage : class
        where TCommand : class, TMessage
        where TEvent : class, TMessage
        where TRequest : class, TMessage
        where TResponse : class, TMessage
    {
        ICanSpecifyAzureServiceBusMessagingEntity<TMessage, TCommand, TEvent, TRequest, TResponse> WithMessagingFactory(IMessagingFactory messagingFactory);
        ICanSpecifyAzureServiceBusMessagingEntity<TMessage, TCommand, TEvent, TRequest, TResponse> WithMessagingFactory(MessagingFactory messagingFactory);
    }

    public interface ICanSpecifyAzureServiceBusMessagingEntity<TMessage, TCommand, TEvent, TRequest, TResponse> : ICanSpecifyEndpointSerializers<TMessage, TCommand, TEvent, TRequest, TResponse>
        where TMessage : class
        where TCommand : class, TMessage
        where TEvent : class, TMessage
        where TRequest : class, TMessage
        where TResponse : class, TMessage
    {
        ICanSpecifyAzureServiceBusMessagingEntity<TMessage, TCommand, TEvent, TRequest, TResponse> UsingQueueFor<T>(string queuePath) where T : class, TMessage;
        ICanSpecifyAzureServiceBusMessagingEntity<TMessage, TCommand, TEvent, TRequest, TResponse> UsingQueueFor<T>(string queuePath, MessageReceiveMode receiveMode) where T : class, TMessage;
        ICanSpecifyAzureServiceBusMessagingEntity<TMessage, TCommand, TEvent, TRequest, TResponse> UsingQueueFor<T>(string queuePath, MessagingEntityCreationOptions creationOptions) where T : class, TMessage;
        ICanSpecifyAzureServiceBusMessagingEntity<TMessage, TCommand, TEvent, TRequest, TResponse> UsingQueueFor<T>(string queuePath, MessageReceiveMode receiveMode, MessagingEntityCreationOptions creationOptions) where T : class, TMessage;
        ICanSpecifyAzureServiceBusMessagingEntity<TMessage, TCommand, TEvent, TRequest, TResponse> UsingTopicFor<T>(string topicPath) where T : class, TMessage;
        ICanSpecifyAzureServiceBusMessagingEntity<TMessage, TCommand, TEvent, TRequest, TResponse> UsingTopicFor<T>(string topicPath, MessagingEntityCreationOptions creationOptions) where T : class, TMessage;
        ICanSpecifyAzureServiceBusMessagingEntity<TMessage, TCommand, TEvent, TRequest, TResponse> UsingSubscriptionFor<T>(string topicPath, string subscriptionName) where T : class, TMessage;
        ICanSpecifyAzureServiceBusMessagingEntity<TMessage, TCommand, TEvent, TRequest, TResponse> UsingSubscriptionFor<T>(string topicPath, string subscriptionName, MessageReceiveMode receiveMode) where T : class, TMessage;
        ICanSpecifyAzureServiceBusMessagingEntity<TMessage, TCommand, TEvent, TRequest, TResponse> UsingSubscriptionFor<T>(string topicPath, string subscriptionName, MessagingEntityCreationOptions creationOptions) where T : class, TMessage;
        ICanSpecifyAzureServiceBusMessagingEntity<TMessage, TCommand, TEvent, TRequest, TResponse> UsingSubscriptionFor<T>(string topicPath, string subscriptionName, MessageReceiveMode receiveMode, MessagingEntityCreationOptions creationOptions) where T : class, TMessage;
    }

    internal class AzureServiceBusQueueFluentConfig<TServiceMessage, TMessage, TCommand, TEvent, TRequest, TResponse> : ICanAddAzureServiceBusServiceName<TMessage, TCommand, TEvent, TRequest, TResponse>, ICanSpecifyAzureServiceBusNamespace<TMessage, TCommand, TEvent, TRequest, TResponse>, ICanSpecifyAzureServiceBusMessagingFactory<TMessage, TCommand, TEvent, TRequest, TResponse>, ICanSpecifyAzureServiceBusMessagingEntity<TMessage, TCommand, TEvent, TRequest, TResponse>, ICanCreateEndpointAsClientOrServer<TMessage, TCommand, TEvent, TRequest, TResponse>, ICanSpecifyEndpointSerializers<TMessage, TCommand, TEvent, TRequest, TResponse>
        where TMessage : class
        where TCommand : class, TMessage
        where TEvent : class, TMessage
        where TRequest : class, TMessage
        where TResponse : class, TMessage
        where TServiceMessage : class
    {
        private readonly ICanAddEndpoint<TMessage, TCommand, TEvent, TRequest, TResponse> _canAddEndpoint;
        private string _serviceName;
        private IMessageSerializer _serializer;
        private IMessageDeserializerFactory _deserializerFactory;
        private Func<Assembly, bool> _assemblyFilter;
        private Func<Type, bool> _typeFilter;
        private readonly List<MessageTypePathMappingDetails> _messageTypePathMappings = new List<MessageTypePathMappingDetails>();
        private IMessagingFactory _messagingFactory;
        private INamespaceManager _namespaceManager;

        public AzureServiceBusQueueFluentConfig(ICanAddEndpoint<TMessage, TCommand, TEvent, TRequest, TResponse> canAddEndpoint)
        {
            _canAddEndpoint = canAddEndpoint;
        }

        public ICanSpecifyAzureServiceBusNamespace<TMessage, TCommand, TEvent, TRequest, TResponse> Named(string serviceName)
        {
            _serviceName = serviceName;
            return this;
        }

        public ICanAddEndpointOrLoggingOrCorrelationOrCreate<TMessage, TCommand, TEvent, TRequest, TResponse> AsClient()
        {
            return _canAddEndpoint.WithClientEndpoints(CreateProvider());
        }

        public ICanAddEndpointOrLoggingOrCorrelationOrCreate<TMessage, TCommand, TEvent, TRequest, TResponse> AsServer()
        {
            return _canAddEndpoint.WithServerEndpoints(CreateProvider());
        }

        public ICanAddEndpointOrLoggingOrCorrelationOrCreate<TMessage, TCommand, TEvent, TRequest, TResponse> AsClientAndServer()
        {
            return _canAddEndpoint.WithEndpoints(CreateProvider());
        }

        private AzureServiceBusQueueEndpointProvider<TServiceMessage, TMessage, TCommand, TEvent, TRequest, TResponse> CreateProvider()
        {
            if(_messagingFactory == null)
            {
                _messagingFactory = new MessagingFactoryWrapper(MessagingFactory.Create(_namespaceManager.Address, _namespaceManager.Settings.TokenProvider));
            }
            
            return new AzureServiceBusQueueEndpointProvider<TServiceMessage, TMessage, TCommand, TEvent, TRequest, TResponse>(_serviceName, _namespaceManager, _messagingFactory, _serializer, _deserializerFactory, _messageTypePathMappings, _assemblyFilter, _typeFilter);
        }

        public ICanSpecifyAzureServiceBusMessagingFactory<TMessage, TCommand, TEvent, TRequest, TResponse> WithConnectionString(string connectionString)
        {
            if(connectionString == null) throw new ArgumentNullException("connectionString");
            
            return WithNamespaceManager(NamespaceManager.CreateFromConnectionString(connectionString));
        }

        public ICanSpecifyAzureServiceBusMessagingFactory<TMessage, TCommand, TEvent, TRequest, TResponse> WithNamespaceManager(INamespaceManager namespaceManager)
        {
            if(namespaceManager == null) throw new ArgumentNullException("namespaceManager");
            
            _namespaceManager = namespaceManager;
            return this;
        }

        public ICanSpecifyAzureServiceBusMessagingFactory<TMessage, TCommand, TEvent, TRequest, TResponse> WithNamespaceManager(NamespaceManager namespaceManager)
        {
            if(namespaceManager == null) throw new ArgumentNullException("namespaceManager");
            
            return WithNamespaceManager(new NamespaceManagerWrapper(namespaceManager));
        }


        public ICanSpecifyAzureServiceBusMessagingEntity<TMessage, TCommand, TEvent, TRequest, TResponse> WithMessagingFactory(IMessagingFactory messagingFactory)
        {
            if(messagingFactory == null) throw new ArgumentNullException("messagingFactory");
            
            _messagingFactory = messagingFactory;
            return this;
        }

        public ICanSpecifyAzureServiceBusMessagingEntity<TMessage, TCommand, TEvent, TRequest, TResponse> WithMessagingFactory(MessagingFactory messagingFactory)
        {
            return WithMessagingFactory(new MessagingFactoryWrapper(messagingFactory));
        }

        public ICanCreateEndpointAsClientOrServer<TMessage, TCommand, TEvent, TRequest, TResponse> SerializedWith(IMessageSerializer serializer, IMessageDeserializerFactory deserializerFactory)
        {
            _serializer = serializer;
            _deserializerFactory = deserializerFactory;
            return this;
        }

        public ICanCreateEndpointAsClientOrServer<TMessage, TCommand, TEvent, TRequest, TResponse> FilterMessageTypeAssemblies(Func<Assembly, bool> assemblyFilter = null, Func<Type, bool> typeFilter = null)
        {
            _assemblyFilter = assemblyFilter;
            _typeFilter = typeFilter;
            return this;
        }

        public ICanSpecifyAzureServiceBusMessagingEntity<TMessage, TCommand, TEvent, TRequest, TResponse> UsingQueueFor<T>(string queuePath) where T : class, TMessage
        {
            return UsingQueueFor<T>(queuePath, MessageReceiveMode.PeekLock);
        }

        public ICanSpecifyAzureServiceBusMessagingEntity<TMessage, TCommand, TEvent, TRequest, TResponse> UsingQueueFor<T>(string queuePath, MessageReceiveMode receiveMode) where T : class, TMessage
        {
            return UsingQueueFor<T>(queuePath, receiveMode, MessagingEntityCreationOptions.None);
        }

        public ICanSpecifyAzureServiceBusMessagingEntity<TMessage, TCommand, TEvent, TRequest, TResponse> UsingQueueFor<T>(string queuePath, MessagingEntityCreationOptions creationOptions) where T : class, TMessage
        {
            return UsingQueueFor<T>(queuePath, MessageReceiveMode.PeekLock, creationOptions);
        }

        public ICanSpecifyAzureServiceBusMessagingEntity<TMessage, TCommand, TEvent, TRequest, TResponse> UsingQueueFor<T>(string queuePath, MessageReceiveMode receiveMode, MessagingEntityCreationOptions creationOptions) where T : class, TMessage
        {
            _messageTypePathMappings.Add(new MessageTypePathMappingDetails(typeof(T), queuePath, MessagingEntityType.Queue, creationOptions, receiveMode));
            return this;
        }

        public ICanSpecifyAzureServiceBusMessagingEntity<TMessage, TCommand, TEvent, TRequest, TResponse> UsingTopicFor<T>(string topicPath) where T : class, TMessage
        {
            return UsingTopicFor<T>(topicPath, MessagingEntityCreationOptions.None);
        }

        public ICanSpecifyAzureServiceBusMessagingEntity<TMessage, TCommand, TEvent, TRequest, TResponse> UsingTopicFor<T>(string topicPath, MessagingEntityCreationOptions creationOptions) where T : class, TMessage
        {
            _messageTypePathMappings.Add(new MessageTypePathMappingDetails(typeof(T), topicPath, MessagingEntityType.Topic, creationOptions));
            return this;
        }

        public ICanSpecifyAzureServiceBusMessagingEntity<TMessage, TCommand, TEvent, TRequest, TResponse> UsingSubscriptionFor<T>(string topicPath, string subscriptionName) where T : class, TMessage
        {
            return UsingSubscriptionFor<T>(topicPath, subscriptionName, MessagingEntityCreationOptions.None);
        }

        public ICanSpecifyAzureServiceBusMessagingEntity<TMessage, TCommand, TEvent, TRequest, TResponse> UsingSubscriptionFor<T>(string topicPath, string subscriptionName, MessageReceiveMode receiveMode) where T : class, TMessage
        {
            return UsingSubscriptionFor<T>(topicPath, subscriptionName, receiveMode, MessagingEntityCreationOptions.None);
        }

        public ICanSpecifyAzureServiceBusMessagingEntity<TMessage, TCommand, TEvent, TRequest, TResponse> UsingSubscriptionFor<T>(string topicPath, string subscriptionName, MessagingEntityCreationOptions creationOptions) where T : class, TMessage
        {
            return UsingSubscriptionFor<T>(topicPath, subscriptionName, MessageReceiveMode.PeekLock, creationOptions);
        }

        public ICanSpecifyAzureServiceBusMessagingEntity<TMessage, TCommand, TEvent, TRequest, TResponse> UsingSubscriptionFor<T>(string topicPath, string subscriptionName, MessageReceiveMode receiveMode, MessagingEntityCreationOptions creationOptions) where T : class, TMessage
        {
            _messageTypePathMappings.Add(new MessageTypePathMappingDetails(typeof(T), topicPath + "/subscriptions/" + subscriptionName, MessagingEntityType.Subscription, creationOptions, receiveMode));
            return this;
        }
    }
}
