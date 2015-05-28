using System;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Obvs.AzureServiceBus.Configuration;
using Obvs.Types;

namespace Obvs.AzureServiceBus.Infrastructure
{
	internal class MessageSessionMessageReceiverWrapper : IMessageSessionMessageReceiver
	{
		private readonly MessageSession _messageSession;

		public MessageSessionMessageReceiverWrapper(MessageSession messageSession)
		{
			_messageSession = messageSession;
		}

		public bool IsClosed
		{
			get
			{
				return _messageSession.IsClosed;
			}
		}

		public MessageReceiveMode Mode
		{
			get
			{
				return _messageSession.ReceiveMode;
			}
		}

		public string SessionId
		{
			get
			{
				return _messageSession.SessionId;
			}
		}

		public void Dispose()
		{
			throw new NotImplementedException();
		}

		public Task<BrokeredMessage> ReceiveAsync()
		{
			throw new NotImplementedException();
		}
	}
}