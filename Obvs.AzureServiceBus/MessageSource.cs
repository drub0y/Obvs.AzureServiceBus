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
        private IMessageBrokeredMessageTable _messageBrokeredMessageTable;

        internal MessageSource(IMessagingEntityFactory messagingEntityFactory, IEnumerable<IMessageDeserializer<TMessage>> deserializers, IMessageBrokeredMessageTable messageBrokeredMessageTable)
        {
            if(messagingEntityFactory == null) throw new ArgumentNullException(nameof(messagingEntityFactory));

            IObservable<BrokeredMessage> brokeredMessages = CreateBrokeredMessageObservableFromMessageReceiver(messagingEntityFactory);

            Initialize(brokeredMessages, deserializers, messageBrokeredMessageTable);
        }

        internal MessageSource(IObservable<BrokeredMessage> brokeredMessages, IEnumerable<IMessageDeserializer<TMessage>> deserializers, IMessageBrokeredMessageTable messageBrokeredMessageTable)
        {
            Initialize(brokeredMessages, deserializers, messageBrokeredMessageTable);
        }

        public IObservable<TMessage> Messages
        {
            get
            {
                return _brokeredMessages
                        .Where(IsCorrectMessageType)
                        .Select(bm => new
                        {
                            BrokeredMessage = bm,
                            DeserializedMessage = Deserialize(bm)
                        })
                        .Do(messageParts => _messageBrokeredMessageTable.SetBrokeredMessageForMessage(messageParts.DeserializedMessage, messageParts.BrokeredMessage))
                        .Select(messageParts => messageParts.DeserializedMessage);
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

        private void Initialize(IObservable<BrokeredMessage> brokeredMessages, IEnumerable<IMessageDeserializer<TMessage>> deserializers, IMessageBrokeredMessageTable messageBrokeredMessageTable)
        {
            if(brokeredMessages == null) throw new ArgumentNullException(nameof(brokeredMessages));
            if(deserializers == null) throw new ArgumentNullException(nameof(deserializers));
            if(messageBrokeredMessageTable == null) throw new ArgumentNullException(nameof(messageBrokeredMessageTable));

            _brokeredMessages = brokeredMessages;
            _deserializers = deserializers.ToDictionary(d => d.GetTypeName());
            _messageBrokeredMessageTable = messageBrokeredMessageTable;
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
