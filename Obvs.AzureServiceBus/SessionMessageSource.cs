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
    public class SessionMessageSource<TMessage> : IMessageSource<TMessage> 
        where TMessage : IMessage
    {
        private IObservable<IMessageSessionMessageReceiver> _messageSessionMessageReceivers;
        private Dictionary<string, IMessageDeserializer<TMessage>> _deserializers;
        private CancellationTokenSource _messageReceiverBrokeredMessageObservableCancellationTokenSource;
        private IBrokeredMessagePeekLockControlProvider _peekLockControlProvider;

        public SessionMessageSource(IMessageSessionReceiver messageSessionReceiver, IEnumerable<IMessageDeserializer<TMessage>> deserializers) 
            : this(messageSessionReceiver, deserializers, BrokeredMessagePeekLockControlProvider.Default)
        {            
        }

        public SessionMessageSource(IObservable<IMessageSessionMessageReceiver> messageSessionMessageReceivers, IEnumerable<IMessageDeserializer<TMessage>> deserializers) 
            : this(messageSessionMessageReceivers, deserializers, BrokeredMessagePeekLockControlProvider.Default)
        {            
        }

        internal SessionMessageSource(IMessageSessionReceiver messageSessionReceiver, IEnumerable<IMessageDeserializer<TMessage>> deserializers, IBrokeredMessagePeekLockControlProvider peekLockControlProvider)
        {
            if(messageSessionReceiver == null) throw new ArgumentNullException("messageSessionReceiver");

            Initialize(messageSessionReceiver.SessionMessages, deserializers, peekLockControlProvider);
        }

        internal SessionMessageSource(IObservable<IMessageSessionMessageReceiver> messageSessionMessageReceivers, IEnumerable<IMessageDeserializer<TMessage>> deserializers, IBrokeredMessagePeekLockControlProvider peekLockControlProvider)
        {
            Initialize(messageSessionMessageReceivers, deserializers, peekLockControlProvider);
        }

        public IObservable<TMessage> Messages
        {
            get
            {
				return _messageSessionMessageReceivers.Select(msmr => m)
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

        private void Initialize(IObservable<IMessageSessionMessageReceiver> messageSessionMessageReceivers, IEnumerable<IMessageDeserializer<TMessage>> deserializers, IBrokeredMessagePeekLockControlProvider peekLockControlProvider)
        {
            if(messageSessionMessageReceivers == null) throw new ArgumentNullException("messageSessionMessageReceivers");
            if(deserializers == null) throw new ArgumentNullException("deserializers");
            if(peekLockControlProvider == null) throw new ArgumentNullException("peekLockControlProvider");

            _messageSessionMessageReceivers = messageSessionMessageReceivers;
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
