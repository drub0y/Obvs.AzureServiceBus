using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.ServiceBus.Messaging;
using Obvs.AzureServiceBus.Infrastructure;
using Obvs.Types;

namespace Obvs.AzureServiceBus
{
    public abstract class PeekLockMessage : IPeekLockMessage
    {
        [NonSerialized]
        private IMessagePeekLockControl _brokeredMessagePeekLockControl;

        [XmlIgnore]
        [IgnoreDataMember]
        internal IMessagePeekLockControl BrokeredMessagePeekLockControl
        {
            get
            {
                return _brokeredMessagePeekLockControl;
            }

            set
            {
#if DEBUG

                if(value == null)
                {
                    throw new ArgumentNullException();
                }

#endif
                
                _brokeredMessagePeekLockControl = value;
            }
        }

        public Task AbandonAsync()
        {
            return _brokeredMessagePeekLockControl.AbandonAsync();
        }

        public Task CompleteAsync()
        {
            return _brokeredMessagePeekLockControl.CompleteAsync();
        }

        public Task RejectAsync(string reasonCode, string description)
        {
            return _brokeredMessagePeekLockControl.DeadLetterAsync(reasonCode, description);
        }

        public Task RenewAsync()
        {
            return _brokeredMessagePeekLockControl.RenewLockAsync();
        }
    }

    public interface IPeekLockMessage : IMessage
    {
        Task AbandonAsync();
        Task CompleteAsync();
        Task RejectAsync(string reasonCode, string description);
        Task RenewAsync();
    }

    public interface IPeekLockCommand : ICommand, IPeekLockMessage
    {
    
    }

    public interface IPeekLockEvent : IEvent, IPeekLockMessage
    {

    }

    public interface IPeekLockRequest : IRequest, IPeekLockMessage
    {

    }

    public interface IPeekLockResponse : IResponse, IPeekLockMessage
    {
    
    }
}
