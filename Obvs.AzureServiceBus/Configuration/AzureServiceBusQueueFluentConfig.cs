using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Obvs.AzureServiceBus.Infrastructure;
using Obvs.Configuration;
using Obvs.Serialization;
using Obvs.Types;

namespace Obvs.AzureServiceBus.Configuration
{
    public interface ICanAddAzureServiceBusServiceName
    {
        ICanSpecifyAzureServiceBusNamespace Named(string serviceName);
    }

    public interface ICanSpecifyAzureServiceBusNamespace
    {
        ICanSpecifyAzureServiceBusMessagingFactory WithConnectionString(string connectionString);
        ICanSpecifyAzureServiceBusMessagingFactory WithNamespaceManager(INamespaceManager namespaceManager);
        ICanSpecifyAzureServiceBusMessagingFactory WithNamespaceManager(NamespaceManager namespaceManager);
    }

    public interface ICanSpecifyAzureServiceBusMessagingFactory : ICanSpecifyAzureServiceBusMessagingEntity
    {
        ICanSpecifyAzureServiceBusMessagingEntity WithMessagingFactory(IMessagingFactory messagingFactory);
        ICanSpecifyAzureServiceBusMessagingEntity WithMessagingFactory(MessagingFactory messagingFactory);
    }

    public interface ICanSpecifyAzureServiceBusMessagingEntity : ICanSpecifyEndpointSerializers
    {
        ICanSpecifyAzureServiceBusMessagingEntity UsingQueueFor<TMessage>(string queuePath) where TMessage : IMessage;
        ICanSpecifyAzureServiceBusMessagingEntity UsingQueueFor<TMessage>(string queuePath, bool createIfDoesntExist) where TMessage : IMessage;
        ICanSpecifyAzureServiceBusMessagingEntity UsingTopicFor<TMessage>(string topicPath) where TMessage : IMessage;        
        ICanSpecifyAzureServiceBusMessagingEntity UsingTopicFor<TMessage>(string topicPath, bool createIfDoesntExist) where TMessage : IMessage;
        ICanSpecifyAzureServiceBusMessagingEntity UsingSubscriptionFor<TMessage>(string topicPath, string subscriptionName) where TMessage : IMessage;
        ICanSpecifyAzureServiceBusMessagingEntity UsingSubscriptionFor<TMessage>(string topicPath, string subscriptionName, bool createIfDoesntExist) where TMessage : IMessage;
        ICanSpecifyAzureServiceBusMessagingEntity UsingTemporaryQueueFor<TMessage>(string queuePath) where TMessage : IMessage;
        ICanSpecifyAzureServiceBusMessagingEntity UsingTemporaryQueueFor<TMessage>(string queuePath, bool canDeleteIfAlreadyExists) where TMessage : IMessage;
        ICanSpecifyAzureServiceBusMessagingEntity UsingTemporaryTopicFor<TMessage>(string topicPath) where TMessage : IMessage;
        ICanSpecifyAzureServiceBusMessagingEntity UsingTemporaryTopicFor<TMessage>(string topicPath, bool canDeleteIfAlreadyExists) where TMessage : IMessage;
        ICanSpecifyAzureServiceBusMessagingEntity UsingTemporarySubscriptionFor<TMessage>(string topicPath) where TMessage : IMessage;
        ICanSpecifyAzureServiceBusMessagingEntity UsingTemporarySubscriptionFor<TMessage>(string topicPath, string subscriptionName) where TMessage : IMessage;
        ICanSpecifyAzureServiceBusMessagingEntity UsingTemporarySubscriptionFor<TMessage>(string topicPath, string subscriptionName, bool canDeleteIfAlreadyExists) where TMessage : IMessage;
    }

    internal class AzureServiceBusQueueFluentConfig<TServiceMessage> : ICanAddAzureServiceBusServiceName, ICanSpecifyAzureServiceBusNamespace, ICanSpecifyAzureServiceBusMessagingFactory, ICanSpecifyAzureServiceBusMessagingEntity, ICanCreateEndpointAsClientOrServer, ICanSpecifyEndpointSerializers
        where TServiceMessage : IMessage
    {
        private readonly ICanAddEndpoint _canAddEndpoint;
        private string _serviceName;
        private IMessageSerializer _serializer;
        private IMessageDeserializerFactory _deserializerFactory;
        private string _assemblyNameContains;
        private readonly List<MessageTypePathMappingDetails> _messageTypePathMappings = new List<MessageTypePathMappingDetails>();
        private IMessagingFactory _messagingFactory;
        private INamespaceManager _namespaceManager;

        public AzureServiceBusQueueFluentConfig(ICanAddEndpoint canAddEndpoint)
        {
            _canAddEndpoint = canAddEndpoint;
        }

        public ICanSpecifyAzureServiceBusNamespace Named(string serviceName)
        {
            _serviceName = serviceName;
            return this;
        }

        public ICanAddEndpointOrLoggingOrCorrelationOrCreate AsClient()
        {
            return _canAddEndpoint.WithClientEndpoints(CreateProvider());
        }

        public ICanAddEndpointOrLoggingOrCorrelationOrCreate AsServer()
        {
            return _canAddEndpoint.WithServerEndpoints(CreateProvider());
        }

        public ICanAddEndpointOrLoggingOrCorrelationOrCreate AsClientAndServer()
        {
            return _canAddEndpoint.WithEndpoints(CreateProvider());
        }

        private AzureServiceBusQueueEndpointProvider<TServiceMessage> CreateProvider()
        {
            if(_messagingFactory == null)
            {
                _messagingFactory = new MessagingFactoryWrapper(MessagingFactory.Create(_namespaceManager.Address, _namespaceManager.Settings.TokenProvider));
            }
            
            return new AzureServiceBusQueueEndpointProvider<TServiceMessage>(_serviceName, _namespaceManager, _messagingFactory, _serializer, _deserializerFactory, _messageTypePathMappings, _assemblyNameContains);
        }

        public ICanSpecifyAzureServiceBusMessagingFactory WithConnectionString(string connectionString)
        {
            if(connectionString == null) throw new ArgumentNullException("connectionString");
            
            return WithNamespaceManager(NamespaceManager.CreateFromConnectionString(connectionString));
        }

        public ICanSpecifyAzureServiceBusMessagingFactory WithNamespaceManager(INamespaceManager namespaceManager)
        {
            if(namespaceManager == null) throw new ArgumentNullException("namespaceManager");
            
            _namespaceManager = namespaceManager;
            return this;
        }

        public ICanSpecifyAzureServiceBusMessagingFactory WithNamespaceManager(NamespaceManager namespaceManager)
        {
            if(namespaceManager == null) throw new ArgumentNullException("namespaceManager");
            
            return WithNamespaceManager(new NamespaceManagerWrapper(namespaceManager));
        }


        public ICanSpecifyAzureServiceBusMessagingEntity WithMessagingFactory(IMessagingFactory messagingFactory)
        {
            if(messagingFactory == null) throw new ArgumentNullException("messagingFactory");
            
            _messagingFactory = messagingFactory;
            return this;
        }

        public ICanSpecifyAzureServiceBusMessagingEntity WithMessagingFactory(MessagingFactory messagingFactory)
        {
            return WithMessagingFactory(new MessagingFactoryWrapper(messagingFactory));
        }

        public ICanCreateEndpointAsClientOrServer SerializedWith(IMessageSerializer serializer, IMessageDeserializerFactory deserializerFactory)
        {
            _serializer = serializer;
            _deserializerFactory = deserializerFactory;
            return this;
        }

        public ICanCreateEndpointAsClientOrServer FilterMessageTypeAssemblies(string assemblyNameContains)
        {
            _assemblyNameContains = assemblyNameContains;
            return this;
        }

        public ICanSpecifyAzureServiceBusMessagingEntity UsingQueueFor<TMessage>(string queuePath) where TMessage : IMessage
        {
            return UsingQueueFor<TMessage>(queuePath, createIfDoesntExist: false);
        }

        public ICanSpecifyAzureServiceBusMessagingEntity UsingQueueFor<TMessage>(string queuePath, bool createIfDoesntExist) where TMessage : IMessage
        {
            _messageTypePathMappings.Add(new MessageTypePathMappingDetails(typeof(TMessage), queuePath, MessagingEntityType.Queue, createIfDoesntExist: createIfDoesntExist));
            return this;
        }

        public ICanSpecifyAzureServiceBusMessagingEntity UsingTopicFor<TMessage>(string topicPath) where TMessage : IMessage
        {
            return UsingTopicFor<TMessage>(topicPath, createIfDoesntExist: false);
        }

        public ICanSpecifyAzureServiceBusMessagingEntity UsingTopicFor<TMessage>(string topicPath, bool createIfDoesntExist) where TMessage : IMessage
        {
            _messageTypePathMappings.Add(new MessageTypePathMappingDetails(typeof(TMessage), topicPath, MessagingEntityType.Topic, createIfDoesntExist: createIfDoesntExist));
            return this;
        }

        public ICanSpecifyAzureServiceBusMessagingEntity UsingSubscriptionFor<TMessage>(string topicPath, string subscriptionName) where TMessage : IMessage
        {
            return UsingSubscriptionFor<TMessage>(topicPath, subscriptionName, createIfDoesntExist: false);
        }

        public ICanSpecifyAzureServiceBusMessagingEntity UsingSubscriptionFor<TMessage>(string topicPath, string subscriptionName, bool createIfDoesntExist) where TMessage : IMessage
        {
            _messageTypePathMappings.Add(new MessageTypePathMappingDetails(typeof(TMessage), topicPath + "/subscriptions/" + subscriptionName, MessagingEntityType.Subscription, createIfDoesntExist: createIfDoesntExist));
            return this;
        }

        public ICanSpecifyAzureServiceBusMessagingEntity UsingTemporaryQueueFor<TMessage>(string queuePath) where TMessage : IMessage
        {
            return UsingTemporaryQueueFor<TMessage>(queuePath, canDeleteIfAlreadyExists: false);
        }
        public ICanSpecifyAzureServiceBusMessagingEntity UsingTemporaryQueueFor<TMessage>(string queuePath, bool canDeleteIfAlreadyExists) where TMessage : IMessage
        {
            _messageTypePathMappings.Add(new MessageTypePathMappingDetails(typeof(TMessage), queuePath, MessagingEntityType.Queue, createIfDoesntExist: true, isTemporary: true, canDeleteTemporaryIfAlreadyExists: canDeleteIfAlreadyExists));
            return this;
        }

        public ICanSpecifyAzureServiceBusMessagingEntity UsingTemporaryTopicFor<TMessage>(string topicPath) where TMessage : IMessage
        {
            return UsingTemporaryTopicFor<TMessage>(topicPath, canDeleteIfAlreadyExists: false);
        }

        public ICanSpecifyAzureServiceBusMessagingEntity UsingTemporaryTopicFor<TMessage>(string topicPath, bool canDeleteIfAlreadyExists) where TMessage : IMessage
        {
            _messageTypePathMappings.Add(new MessageTypePathMappingDetails(typeof(TMessage), topicPath, MessagingEntityType.Topic, createIfDoesntExist: true, isTemporary: true, canDeleteTemporaryIfAlreadyExists: canDeleteIfAlreadyExists));
            return this;
        }

        public ICanSpecifyAzureServiceBusMessagingEntity UsingTemporarySubscriptionFor<TMessage>(string topicPath) where TMessage : IMessage
        {
            string temporarySubscriptionName = Guid.NewGuid().ToString("D");
            
            return UsingTemporarySubscriptionFor<TMessage>(topicPath, temporarySubscriptionName, canDeleteIfAlreadyExists: true);
        }

        public ICanSpecifyAzureServiceBusMessagingEntity UsingTemporarySubscriptionFor<TMessage>(string topicPath, string subscriptionName) where TMessage : IMessage
        {
            return UsingTemporarySubscriptionFor<TMessage>(topicPath, subscriptionName, canDeleteIfAlreadyExists: false);
        }

        public ICanSpecifyAzureServiceBusMessagingEntity UsingTemporarySubscriptionFor<TMessage>(string topicPath, string subscriptionName, bool canDeleteIfAlreadyExists) where TMessage : IMessage
        {
            _messageTypePathMappings.Add(new MessageTypePathMappingDetails(typeof(TMessage), topicPath + "/subscriptions/" + subscriptionName, MessagingEntityType.Subscription, createIfDoesntExist: true, isTemporary: true, canDeleteTemporaryIfAlreadyExists: canDeleteIfAlreadyExists));
            return this;
        }
    }
}
