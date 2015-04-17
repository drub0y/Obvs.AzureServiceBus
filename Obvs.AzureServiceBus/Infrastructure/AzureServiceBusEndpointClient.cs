using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Obvs.Types;

namespace Obvs.AzureServiceBus.Infrastructure
{
    internal class AzureServiceBusEndpointClient<TServiceMessage> : ServiceEndpointClient where TServiceMessage : IMessage
    {
        private IMessageReceiver _messageReceiver;
        private IMessageSender _messageSender;

        public AzureServiceBusEndpointClient(IMessageReceiver messageReceiver, IMessageSessionReceiver messageSessionReceiver, IMessageSender messageSender)
        {
            _messageReceiver = messageReceiver;
            _messageSessionReceiver = messageSessionReceiver;
            _messageSender = messageSender;
        }

        public IObservable<IEvent> Events
        {
            get
            {
                return Observable.Create<TServiceMessage>(o =>
                {
                    return (from bm in CreateBrokeredMessageObservableFromMessageReceiver()
                            where IsCorrectMessageType(bm)
                            select new
                            {
                                BrokeredMessage = bm,
                                DeserializedMessage = Deserialize(bm)
                            })
                        .Subscribe(
                            messageParts =>
                            {
                                o.OnNext(messageParts.DeserializedMessage);

                                if(_shouldAutoCompleteMessages)
                                {
                                    AutoCompleteBrokeredMessage(messageParts.BrokeredMessage);
                                }
                            },
                            o.OnError,
                            o.OnCompleted);
                });
            }
        }

        public IObservable<IResponse> GetResponses(IRequest request)
        {
            
        }

        public Task SendAsync(ICommand command)
        {
            _messageSender.SendAsync(new BrokeredMessage)
        }

        public bool CanHandle(IMessage message)
        {
            return message is TServiceMessage;
        }

        private IObservable<BrokeredMessage> CreateBrokeredMessageObservableFromMessageReceiver()
        {
            CancellationTokenSource messageReceiverBrokeredMessageObservableCancellationTokenSource = new CancellationTokenSource();

            IObservable<BrokeredMessage> brokeredMessages = Observable.Create<BrokeredMessage>(async (observer, cancellationToken) =>
            {
                while(!_messageReceiver.IsClosed
                            &&
                        !cancellationToken.IsCancellationRequested
                            &&
                       !messageReceiverBrokeredMessageObservableCancellationTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        BrokeredMessage nextMessage = await _messageReceiver.ReceiveAsync();

                        if(nextMessage != null)
                        {
                            observer.OnNext(nextMessage);
                        }
                    }
                    catch(Exception exception)
                    {
                        observer.OnError(exception);
                    }
                }

                observer.OnCompleted();

                return new CancellationDisposable(messageReceiverBrokeredMessageObservableCancellationTokenSource);
            });

            return brokeredMessages.Publish().RefCount();
        }
    }
}
