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
    public class AzureServiceBusQueueEndpointProvider<TServiceMessage> : ServiceEndpointProviderBase where TServiceMessage : IMessage
    {
        private readonly IMessageSerializer _serializer;
        private readonly IMessageDeserializerFactory _deserializerFactory;
        private readonly string _assemblyNameContains;
        private readonly MessageClientEntityFactory _messageClientEntityFactory;
        private readonly IDictionary<Type, List<object>> _messagePropertyProviders;

        public AzureServiceBusQueueEndpointProvider(string serviceName, INamespaceManager namespaceManager, IMessagingFactory messagingFactory, IMessageSerializer serializer, IMessageDeserializerFactory deserializerFactory, List<MessageTypePathMappingDetails> messageTypePathMappings, string assemblyNameContains, IDictionary<Type, List<object>> messagePropertyProviders)
            : base(serviceName)
        {
            _serializer = serializer;
            _deserializerFactory = deserializerFactory;
            _assemblyNameContains = assemblyNameContains;
            _messageClientEntityFactory = new MessageClientEntityFactory(namespaceManager, messagingFactory, messageTypePathMappings);
            _messagePropertyProviders = messagePropertyProviders;
        }

        public override IServiceEndpoint CreateEndpoint()
        {
            return new ServiceEndpoint(
               new MessageSource<IRequest>(_messageClientEntityFactory.CreateMessageReceiver<IRequest>(), _deserializerFactory.Create<IRequest, TServiceMessage>(_assemblyNameContains)),
               new MessageSource<ICommand>(_messageClientEntityFactory.CreateMessageReceiver<ICommand>(), _deserializerFactory.Create<ICommand, TServiceMessage>(_assemblyNameContains)),
               new MessagePublisher<IEvent>(_messageClientEntityFactory.CreateMessageSender<IEvent>(), _serializer, this.BuildPropertyProviderForMessageProvidersOfType<IEvent>()),
               new MessagePublisher<IResponse>(_messageClientEntityFactory.CreateMessageSender<IResponse>(), _serializer, this.BuildPropertyProviderForMessageProvidersOfType<IResponse>()),
               typeof(TServiceMessage));
        }

        public override IServiceEndpointClient CreateEndpointClient()
        {
            return new ServiceEndpointClient(
               new MessageSource<IEvent>(_messageClientEntityFactory.CreateMessageReceiver<IEvent>(), _deserializerFactory.Create<IEvent, TServiceMessage>(_assemblyNameContains)),
               new MessageSource<IResponse>(_messageClientEntityFactory.CreateMessageReceiver<IResponse>(), _deserializerFactory.Create<IResponse, TServiceMessage>(_assemblyNameContains)),
               new MessagePublisher<IRequest>(_messageClientEntityFactory.CreateMessageSender<IRequest>(), _serializer, this.BuildPropertyProviderForMessageProvidersOfType<IRequest>()),
               new MessagePublisher<ICommand>(_messageClientEntityFactory.CreateMessageSender<ICommand>(), _serializer, this.BuildPropertyProviderForMessageProvidersOfType<ICommand>()),
               typeof(TServiceMessage));
        }

        private IMessagePropertyProvider<TMessage> BuildPropertyProviderForMessageProvidersOfType<TMessage>() where TMessage : IMessage
        {
            return new DispatchingPropertyProvider<TMessage>(_messagePropertyProviders);
        }
    }    

    internal sealed class DispatchingPropertyProvider<TMessage> : IMessagePropertyProvider<TMessage> where TMessage : IMessage
    {
        private IDictionary<Type, List<object>> _providers;

        public DispatchingPropertyProvider(IDictionary<Type, List<object>> providers)
        {
            _providers = providers;
        }

        #region IMessagePropertyProvider<TMessage> Members

        public IEnumerable<KeyValuePair<string, object>> GetProperties(TMessage message)
        {
            Type messageType = message.GetType();
            
            IEnumerable<Type> types = messageType.FindInterfaces((t, fc) => typeof(TMessage).IsAssignableFrom(t), null);

            if(messageType.IsClass)
            {
                types = types.Concat(GetMessageTypeHierarchy(messageType));
            }
                
            object[] getPropertiesMethodParameters = new object[] { message };
    
            foreach(Type type in types)
            {
                List<object> providersForType;
                
                if(_providers.TryGetValue(type, out providersForType))
                {
                    MethodInfo getPropertiesMethodInfo = typeof(IMessagePropertyProvider<>).MakeGenericType(type).GetMethod("GetProperties");
                
                    foreach(object providerForType in providersForType)
                    {
                        IEnumerable<KeyValuePair<string, object>> properties = (IEnumerable<KeyValuePair<string, object>>)getPropertiesMethodInfo.Invoke(providerForType, getPropertiesMethodParameters);

                        foreach(KeyValuePair<string, object> property in properties)
                        {
                            yield return property;
                        }
                    }
                }
            }
        }

        private static IEnumerable<Type> GetMessageTypeHierarchy(Type messageType)
        {
            Type nextType = messageType;

            do
            {
                yield return nextType;

                nextType = nextType.BaseType;
            } while(nextType != typeof(object));
            
        }

        #endregion
    }
}
