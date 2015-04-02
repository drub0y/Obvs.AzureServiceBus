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
        public static ICanAddAzureServiceBusServiceName WithAzureServiceBusEndpoint<TServiceMessage>(this ICanAddEndpoint canAddEndpoint) where TServiceMessage : IMessage
        {
            return new AzureServiceBusQueueFluentConfig<TServiceMessage>(canAddEndpoint);
        }
    }
}
