using Obvs.Types;
using System;

namespace Obvs.AzureServiceBus.Infrastructure
{
	public interface IMessageSessionReceiver
	{
		IObservable<IMessageSessionMessageReceiver> SessionMessages
		{
			get;
		}
	}
}