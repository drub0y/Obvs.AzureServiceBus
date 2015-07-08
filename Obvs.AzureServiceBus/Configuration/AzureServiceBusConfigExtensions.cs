using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Obvs.Configuration;
using Obvs.Types;

namespace Obvs.AzureServiceBus.Configuration
{
    public static class AzureServiceBusConfigExtensions
    {
        public static ICanAddAzureServiceBusServiceName<TMessage, TCommand, TEvent, TRequest, TResponse> WithAzureServiceBusEndpoint<TServiceMessage, TMessage, TCommand, TEvent, TRequest, TResponse>(this ICanAddEndpoint<TMessage, TCommand, TEvent, TRequest, TResponse> canAddEndpoint) 
            where TMessage : class
            where TServiceMessage : class
            where TCommand : class, TMessage
            where TEvent : class, TMessage
            where TRequest : class, TMessage
            where TResponse : class, TMessage
        {
            return new AzureServiceBusFluentConfig<TServiceMessage, TMessage, TCommand, TEvent, TRequest, TResponse>(canAddEndpoint);
        }

        public static ICanAddAzureServiceBusServiceName<IMessage, ICommand, IEvent, IRequest, IResponse> WithAzureServiceBusEndpoint<TServiceMessage>(this ICanAddEndpoint<IMessage, ICommand, IEvent, IRequest, IResponse> canAddEndpoint) where TServiceMessage : class
        {
            return canAddEndpoint.WithAzureServiceBusEndpoint<TServiceMessage, IMessage, ICommand, IEvent, IRequest, IResponse>();
        }
    }
}
