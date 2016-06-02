using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using Microsoft.ServiceBus.Messaging;
using Obvs.AzureServiceBus.Infrastructure;
using Obvs.MessageProperties;
using Obvs.Serialization;

namespace Obvs.AzureServiceBus
{
    public class MessageSource<TMessage> : IMessageSource<TMessage>
        where TMessage : class
    {
        private IObservable<BrokeredMessage> _brokeredMessages;
        private Dictionary<string, IMessageDeserializer<TMessage>> _deserializers;
        private CancellationTokenSource _messageReceiverBrokeredMessageObservableCancellationTokenSource;
        private IBrokeredMessagePeekLockControlProvider _peekLockControlProvider;

        public MessageSource(IMessagingEntityFactory messagingEntityFactory, IEnumerable<IMessageDeserializer<TMessage>> deserializers)
            : this(messagingEntityFactory, deserializers, BrokeredMessagePeekLockControlProvider.Default)
        {
        }

        public MessageSource(IObservable<BrokeredMessage> brokeredMessages, IEnumerable<IMessageDeserializer<TMessage>> deserializers)
            : this(brokeredMessages, deserializers, BrokeredMessagePeekLockControlProvider.Default)
        {
        }

        internal MessageSource(IMessagingEntityFactory messagingEntityFactory, IEnumerable<IMessageDeserializer<TMessage>> deserializers, IBrokeredMessagePeekLockControlProvider peekLockControlProvider)
        {
            if(messagingEntityFactory == null) throw new ArgumentNullException(nameof(messagingEntityFactory));

            IObservable<BrokeredMessage> brokeredMessages = CreateBrokeredMessageObservableFromMessageReceiver(messagingEntityFactory);

            Initialize(brokeredMessages, deserializers, peekLockControlProvider);
        }

        internal MessageSource(IObservable<BrokeredMessage> brokeredMessages, IEnumerable<IMessageDeserializer<TMessage>> deserializers, IBrokeredMessagePeekLockControlProvider peekLockControlProvider)
        {
            Initialize(brokeredMessages, deserializers, peekLockControlProvider);
        }

        public IObservable<TMessage> Messages
        {
            get
            {
                return (from bm in _brokeredMessages
                        where IsCorrectMessageType(bm)
                        select new
                        {
                            BrokeredMessage = bm,
                            DeserializedMessage = Deserialize(bm)
                        })
                        .Select(messageParts =>
                        {
                            TMessage deserializedMessage = messageParts.DeserializedMessage;
                            PeekLockMessage peekLockMessage = messageParts.DeserializedMessage as PeekLockMessage;

                            if(peekLockMessage != null)
                            {
                                // NOTE: in the case of a peek lock message, the peek lock control will take care of disposing the brokered message
                                peekLockMessage.PeekLockControl = _peekLockControlProvider.ProvidePeekLockControl(messageParts.BrokeredMessage);
                            }
                            else
                            {
                                // Dispose of the brokered message immediately as there will be no further use of it
                                messageParts.BrokeredMessage.Dispose();
                            }

                            return deserializedMessage;
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

        private IObservable<BrokeredMessage> CreateBrokeredMessageObservableFromMessageReceiver(IMessagingEntityFactory messagingEntityFactory)
        {
            _messageReceiverBrokeredMessageObservableCancellationTokenSource = new CancellationTokenSource();

            return Observable.Create<BrokeredMessage>(async (observer, cancellationToken) =>
            {
                IMessageReceiver messageReceiver = messagingEntityFactory.CreateMessageReceiver(typeof(TMessage));

                while(!cancellationToken.IsCancellationRequested
                            &&
                       !_messageReceiverBrokeredMessageObservableCancellationTokenSource.IsCancellationRequested
                            &&
                       !messageReceiver.IsClosed)
                {
                    BrokeredMessage nextMessage = await messageReceiver.ReceiveAsync(); // NOTE: could pass the CancellationToken in here if ReceiveAsync is ever updated to accept it

                    if(nextMessage != null)
                    {
                        observer.OnNext(nextMessage);
                    }
                }

                return () => messageReceiver.Dispose();
            }).Publish().RefCount();
        }

        private void Initialize(IObservable<BrokeredMessage> brokeredMessages, IEnumerable<IMessageDeserializer<TMessage>> deserializers, IBrokeredMessagePeekLockControlProvider peekLockControlProvider)
        {
            if(brokeredMessages == null) throw new ArgumentNullException(nameof(brokeredMessages));
            if(deserializers == null) throw new ArgumentNullException(nameof(deserializers));
            if(peekLockControlProvider == null) throw new ArgumentNullException(nameof(peekLockControlProvider));

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

            return messageDeserializerForType.Deserialize(message.GetBody<Stream>());
        }
    }
}
