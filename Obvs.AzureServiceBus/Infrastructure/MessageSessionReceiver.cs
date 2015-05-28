using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Obvs.AzureServiceBus.Configuration;
using Obvs.Types;
using System;
using System.Reactive.Linq;

namespace Obvs.AzureServiceBus.Infrastructure
{
    internal sealed class MessageSessionReceiver<TMessage> : IMessageSessionReceiver<TMessage>
		where TMessage : IMessage
    {
		private readonly IObservable<MessageSession> _messageSessions;

        public MessageSessionReceiver(IObservable<MessageSession> messageSessions)
        {
            _messageSessions = messageSessions;
        }

        public IObservable<IMessageSession<TMessage>> Sessions
        {
            get
			{
				return _messageSessions.Select(ms => new MessageSessionMessageReceiverWrapper(ms)).Publish().RefCount();
			}
        }
    }
}
