using System.Collections.Generic;
using Obvs.AzureServiceBus.Configuration;

namespace Obvs.AzureServiceBus.Infrastructure
{
    public interface IMessagingEntityVerifier
    {
        void EnsureMessagingEntitiesExist(IEnumerable<MessageTypeMessagingEntityMappingDetails> messageTypePathMappings);
    }
}