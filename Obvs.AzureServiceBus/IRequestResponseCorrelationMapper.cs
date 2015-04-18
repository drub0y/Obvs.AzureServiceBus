using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Obvs.Types;

namespace Obvs.AzureServiceBus
{
    public interface IBrokeredMessageRequestResponseCorrelationMapper
    {
        void MapFromRequest(IRequest request, BrokeredMessage brokeredMessage);
        void MapToResponse(BrokeredMessage brokeredMessage, IResponse response);
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

        public void MapToResponse(BrokeredMessage brokeredMessage, IResponse response)
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
