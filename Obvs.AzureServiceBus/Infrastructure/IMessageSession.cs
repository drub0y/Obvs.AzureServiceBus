using Obvs.Types;
using System;

namespace Obvs.AzureServiceBus.Infrastructure
{
	public interface IMessageSessionMessageReceiver : IMessageReceiver
	{
		string SessionId
		{
			get;
		}
	}
}