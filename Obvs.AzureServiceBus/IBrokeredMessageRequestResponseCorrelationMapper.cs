using Microsoft.ServiceBus.Messaging;
using Obvs.Types;

namespace Obvs.AzureServiceBus
{
    public interface IBrokeredMessageRequestResponseCorrelationMapper
    {
        void MapFromRequest(IRequest request, BrokeredMessage brokeredMessage);
        void MapFromResponse(IResponse response, BrokeredMessage brokeredMessage);
    }

    public sealed class DefaultBrokeredMessageRequestResponseCorrelationMapper : IBrokeredMessageRequestResponseCorrelationMapper
    {
        public void MapFromRequest(IRequest request, BrokeredMessage brokeredMessage)
        {
            string requesterId = request.RequesterId;

            if(!string.IsNullOrEmpty(requesterId))
            {
                brokeredMessage.ReplyToSessionId = requesterId;
            }

            brokeredMessage.CorrelationId = request.RequestId;
        }

        public void MapFromResponse(IResponse response, BrokeredMessage brokeredMessage)
        {
            string requesterId = response.RequesterId;

            if(!string.IsNullOrEmpty(requesterId))
            {
                brokeredMessage.SessionId = requesterId;
            }

            brokeredMessage.CorrelationId = response.RequestId;
        }
    }

}
