using System;
using System.Collections.Generic;
using Microsoft.ServiceBus.Messaging;
using Obvs.Configuration;
using Obvs.Serialization;
using Obvs.Types;

namespace Obvs.AzureServiceBus.Configuration
{
    public interface ICanAddAzureServiceBusServiceName
    {
        ICanSpecifyAzureServiceBusConnectionSettings Named(string serviceName);
    }

    public interface ICanSpecifyAzureServiceBusConnectionSettings
    {
        ICanSpecifyAzureServiceBusMessagingSettings WithConnectionString(string connectionString);
    }

    public interface ICanSpecifyAzureServiceBusMessagingSettings : ICanSpecifyAzureServiceBusMessagingEntity
    {
        ICanSpecifyAzureServiceBusMessagingEntity WithMessagingSettings(MessagingFactorySettings settings);
    }

    public interface ICanSpecifyAzureServiceBusMessagingEntity : ICanSpecifyEndpointSerializers
    {
        ICanSpecifyAzureServiceBusMessagingEntity UsingQueueFor<TMessage>(string queuePath) where TMessage : IMessage;
        ICanSpecifyAzureServiceBusMessagingEntity UsingTopicFor<TMessage>(string topicPath) where TMessage : IMessage;
        ICanSpecifyAzureServiceBusMessagingEntity UsingSubscriptionFor<TMessage>(string subscriptionPath) where TMessage : IMessage;
    }

    internal class AzureServiceBusQueueFluentConfig<TServiceMessage> : ICanAddAzureServiceBusServiceName, ICanSpecifyAzureServiceBusConnectionSettings, ICanSpecifyAzureServiceBusMessagingSettings, ICanSpecifyAzureServiceBusMessagingEntity, ICanCreateEndpointAsClientOrServer, ICanSpecifyEndpointSerializers
        where TServiceMessage : IMessage
    {
        private readonly ICanAddEndpoint _canAddEndpoint;
        private string _serviceName;
        private Uri _connectionString;
        private MessagingFactorySettings _settings;
        private IMessageSerializer _serializer;
        private IMessageDeserializerFactory _deserializerFactory;
        private readonly List<MessageTypePathMappingDetails> _messageTypePathMappings = new List<MessageTypePathMappingDetails>();

        public AzureServiceBusQueueFluentConfig(ICanAddEndpoint canAddEndpoint)
        {
            _canAddEndpoint = canAddEndpoint;
        }

        public ICanSpecifyAzureServiceBusConnectionSettings Named(string serviceName)
        {
            _serviceName = serviceName;
            return this;
        }

        public ICanAddEndpointOrLoggingOrCreate AsClient()
        {
            return _canAddEndpoint.WithClientEndpoints(CreateProvider());
        }

        public ICanAddEndpointOrLoggingOrCreate AsServer()
        {
            return _canAddEndpoint.WithServerEndpoints(CreateProvider());
        }

        public ICanAddEndpointOrLoggingOrCreate AsClientAndServer()
        {
            return _canAddEndpoint.WithEndpoints(CreateProvider());
        }

        private AzureServiceBusQueueEndpointProvider<TServiceMessage> CreateProvider()
        {
            return new AzureServiceBusQueueEndpointProvider<TServiceMessage>(_serviceName, _connectionString, _serializer, _deserializerFactory, _messageTypePathMappings, _settings);
        }

        public ICanSpecifyAzureServiceBusMessagingSettings WithConnectionString(Uri connectionString)
        {
            _connectionString = connectionString;
            return this;
        }

        public ICanSpecifyAzureServiceBusMessagingSettings WithConnectionString(string connectionString)
        {
            _connectionString = new Uri(connectionString);
            return this;
        }

        public ICanSpecifyAzureServiceBusMessagingEntity WithMessagingSettings(MessagingFactorySettings settings)
        {
            _settings = settings;
            return this;
        }

        public ICanCreateEndpointAsClientOrServer SerializedWith(IMessageSerializer serializer, IMessageDeserializerFactory deserializerFactory)
        {
            _serializer = serializer;
            _deserializerFactory = deserializerFactory;
            return this;
        }

        public ICanSpecifyAzureServiceBusMessagingEntity UsingQueueFor<TMessage>(string queuePath) where TMessage : IMessage
        {
            _messageTypePathMappings.Add(new MessageTypePathMappingDetails(typeof(TMessage), queuePath, MessagingEntityType.Queue));
            return this;
        }

        public ICanSpecifyAzureServiceBusMessagingEntity UsingTopicFor<TMessage>(string topicPath) where TMessage : IMessage
        {
            _messageTypePathMappings.Add(new MessageTypePathMappingDetails(typeof(TMessage), topicPath, MessagingEntityType.Topic));
            return this;
        }

        public ICanSpecifyAzureServiceBusMessagingEntity UsingSubscriptionFor<TMessage>(string subscriptionPath) where TMessage : IMessage
        {
            _messageTypePathMappings.Add(new MessageTypePathMappingDetails(typeof(TMessage), subscriptionPath, MessagingEntityType.Subscription));
            return this;
        }

        public ICanCreateEndpointAsClientOrServer FilterMessageTypeAssemblies(string assemblyNameContains)
        {
            throw new NotImplementedException();
        }
    }
}
