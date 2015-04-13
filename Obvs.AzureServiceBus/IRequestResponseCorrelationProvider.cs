using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Obvs.Types;

namespace Obvs.AzureServiceBus
{
    public interface IRequestResponseCorrelationProvider
    {
        void Correlate(IRequest request, BrokeredMessage brokeredMessage);
        void Correlate(IResponse response, BrokeredMessage brokeredMessage);
    }

    public sealed class DefaultRequestResponseCorrelationProvider : IRequestResponseCorrelationProvider
    {
        public void Correlate(IRequest request, BrokeredMessage brokeredMessage)
        {
            string requesterId = request.RequesterId;

            if(!string.IsNullOrEmpty(requesterId))
            {
                brokeredMessage.ReplyToSessionId = requesterId;
            }

            brokeredMessage.CorrelationId = request.RequestId;
        }

        public void Correlate(IResponse response, BrokeredMessage brokeredMessage)
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
