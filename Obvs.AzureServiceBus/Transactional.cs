using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.ServiceBus.Messaging;
using Obvs.Types;

namespace Obvs.AzureServiceBus
{
    public abstract class TransactionalMessage : ITransactionalMessage
    {
        [NonSerialized]
        private BrokeredMessage _brokeredMessage;

        [XmlIgnore]
        [IgnoreDataMember]
        internal BrokeredMessage BrokeredMessage
        {
            get
            {
                return _brokeredMessage;
            }

            set
            {
#if DEBUG

                if(value == null)
                {
                    throw new ArgumentNullException();
                }

#endif
                
                _brokeredMessage = value;
            }
        }
        public Task AbandonAsync()
        {
            return _brokeredMessage.AbandonAsync();
        }

        public Task CompleteAsync()
        {
            return _brokeredMessage.CompleteAsync();
        }

        public Task RejectAsync(string reasonCode, string description)
        {
            return _brokeredMessage.DeadLetterAsync(reasonCode, description);
        }

        public Task RenewAsync()
        {
            return _brokeredMessage.RenewLockAsync();
        }
    }

    public interface ITransactionalMessage : IMessage
    {
        Task AbandonAsync();
        Task CompleteAsync();
        Task RejectAsync(string reasonCode, string description);
        Task RenewAsync();
    }

    public interface ITransactionalCommand : ICommand, ITransactionalMessage
    {
    
    }

    public interface ITransactionalEvent : IEvent, ITransactionalMessage
    {

    }

    public interface ITransactionalRequest : IRequest, ITransactionalMessage
    {

    }

    public interface ITransactionalResponse : IResponse, ITransactionalMessage
    {
    
    }
}
