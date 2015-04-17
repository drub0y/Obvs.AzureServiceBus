using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Obvs.Types;

namespace Obvs.AzureServiceBus.Infrastructure
{
    internal class AzureServiceBusServiceEndpoint<TServiceMessage> : IServiceEndpoint where TServiceMessage : IMessage
    {
        public AzureServiceBusServiceEndpoint()
        {

        }

        public IObservable<ICommand> Commands
        {
            get
            {
                
            }
        }

        public Task PublishAsync(IEvent ev)
        {
            
        }

        public Task ReplyAsync(IRequest request, IResponse response)
        {
            
        }

        public IObservable<IRequest> Requests
        {
            get
            {
                
            }
        }

        public bool CanHandle(IMessage message)
        {
            return message is TServiceMessage;
        }
    }
}
