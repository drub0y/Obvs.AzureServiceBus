using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;
using Obvs.AzureServiceBus.Infrastructure;
using Obvs.MessageProperties;
using Obvs.Serialization;
using Obvs.Types;

namespace Obvs.AzureServiceBus
{
    public class MessageSource<TMessage> : IMessageSource<TMessage> 
        where TMessage : IMessage
    {
        private IObservable<BrokeredMessage> _brokeredMessages;
        private Dictionary<string, IMessageDeserializer<TMessage>> _deserializers;
        private CancellationTokenSource _messageReceiverBrokeredMessageObservableCancellationTokenSource;
        private IMessagePeekLockControlProvider _peekLockControlProvider;

        public MessageSource(IMessageReceiver messageReceiver, IEnumerable<IMessageDeserializer<TMessage>> deserializers) 
            : this(messageReceiver, deserializers, MessagePeekLockControlProvider.Default)
        {            
        }

        public MessageSource(IObservable<BrokeredMessage> brokeredMessages, IEnumerable<IMessageDeserializer<TMessage>> deserializers) 
            : this(brokeredMessages, deserializers, MessagePeekLockControlProvider.Default)
        {            
        }

        internal MessageSource(IMessageReceiver messageReceiver, IEnumerable<IMessageDeserializer<TMessage>> deserializers, IMessagePeekLockControlProvider peekLockControlProvider)
        {
            if(messageReceiver == null) throw new ArgumentNullException("messageReceiver");

            IObservable<BrokeredMessage> brokeredMessages = CreateBrokeredMessageObservableFromMessageReceiver(messageReceiver);

            Initialize(brokeredMessages, deserializers, peekLockControlProvider);
        }

        internal MessageSource(IObservable<BrokeredMessage> brokeredMessages, IEnumerable<IMessageDeserializer<TMessage>> deserializers, IMessagePeekLockControlProvider peekLockControlProvider)
        {
            Initialize(brokeredMessages, deserializers, peekLockControlProvider);
        }

        public IObservable<TMessage> Messages
        {
            get
            {
                return Observable.Create<TMessage>(o =>
                    {
                        return (from bm in _brokeredMessages
                                where IsCorrectMessageType(bm)
                                select new
                                {
                                    BrokeredMessage = bm,
                                    DeserializedMessage = Deserialize(bm)
                                })
                            .Subscribe(
                                messageParts =>
                                {
                                    PeekLockMessage transactionalMessage = messageParts.DeserializedMessage as PeekLockMessage;

                                    if(transactionalMessage != null)
                                    {
                                        transactionalMessage.BrokeredMessagePeekLockControl = _peekLockControlProvider.ProvidePeekLockControl(messageParts.BrokeredMessage);
                                    }
                                    
                                    o.OnNext(messageParts.DeserializedMessage);
                                },
                                o.OnError,
                                o.OnCompleted);
                    });
            }
        }

        public void Dispose()
        {
            if(_messageReceiverBrokeredMessageObservableCancellationTokenSource != null)
            {
                _messageReceiverBrokeredMessageObservableCancellationTokenSource.Cancel();
                _messageReceiverBrokeredMessageObservableCancellationTokenSource.Dispose();
                _messageReceiverBrokeredMessageObservableCancellationTokenSource = null;
            }
        }

        private IObservable<BrokeredMessage> CreateBrokeredMessageObservableFromMessageReceiver(IMessageReceiver messageReceiver)
        {
            _messageReceiverBrokeredMessageObservableCancellationTokenSource = new CancellationTokenSource();
            
            IObservable<BrokeredMessage> brokeredMessages = Observable.Create<BrokeredMessage>(async (observer, cancellationToken) =>
            {
                while(!messageReceiver.IsClosed
                            &&
                       !cancellationToken.IsCancellationRequested
                            &&
                       !_messageReceiverBrokeredMessageObservableCancellationTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        BrokeredMessage nextMessage = await messageReceiver.ReceiveAsync(); // NOTE: could pass CancellationToken in here if ReceiveAsync is ever updated to accept it

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

                return Disposable.Empty;
            });

            return brokeredMessages.Publish().RefCount();
        }

        private void Initialize(IObservable<BrokeredMessage> brokeredMessages, IEnumerable<IMessageDeserializer<TMessage>> deserializers, IMessagePeekLockControlProvider peekLockControlProvider)
        {
            if(brokeredMessages == null) throw new ArgumentNullException("brokeredMessages");
            if(deserializers == null) throw new ArgumentNullException("deserializers");

            _brokeredMessages = brokeredMessages;
            _deserializers = deserializers.ToDictionary(d => d.GetTypeName());
            _peekLockControlProvider = peekLockControlProvider;
        }

        private bool IsCorrectMessageType(BrokeredMessage brokeredMessage)
        {
            object messageTypeName;
            bool messageTypeMatches = brokeredMessage.Properties.TryGetValue(MessagePropertyNames.TypeName, out messageTypeName);

            if(messageTypeMatches)
            {
                messageTypeMatches = _deserializers.ContainsKey((string)messageTypeName);
            }

            return messageTypeMatches;
        }

        private TMessage Deserialize(BrokeredMessage message)
        {
            object messageTypeName;
            IMessageDeserializer<TMessage> messageDeserializerForType;
            
            if(message.Properties.TryGetValue(MessagePropertyNames.TypeName, out messageTypeName))
            {
                messageDeserializerForType = _deserializers[(string)messageTypeName];
            }
            else
            {
                try
                {
                    messageDeserializerForType = _deserializers.Values.Single();
                }
                catch(InvalidOperationException exception)
                {
                    throw new Exception("The message contained no explicit TypeName property. In this scenario there must be a single deserializer provided.", exception);
                }
            }
            
            TMessage deserializedMessage = messageDeserializerForType.Deserialize(message.GetBody<Stream>());

            return deserializedMessage;
        }

        private static void AutoCompleteBrokeredMessage(BrokeredMessage brokeredMessage)
        {
            brokeredMessage.CompleteAsync().ContinueWith(completeAntecedent =>
            {
                // TODO: figure out how to get an ILogger in here and log failures
            },
            TaskContinuationOptions.OnlyOnFaulted);
        }

    }
}
