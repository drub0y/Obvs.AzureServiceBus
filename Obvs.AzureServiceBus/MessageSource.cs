using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using HackedBrain.WindowsAzure.ServiceBus.Messaging;
using Microsoft.ServiceBus.Messaging;
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
        private bool _shouldAutoCompleteMessages;        

        public MessageSource(MessageReceiver messageReceiver, IEnumerable<IMessageDeserializer<TMessage>> deserializers)
            : this(messageReceiver, deserializers, true)
        {
        }

        public MessageSource(MessageReceiver messageReceiver, IEnumerable<IMessageDeserializer<TMessage>> deserializers, bool shouldAutoCompleteMessages)
        {
            if(messageReceiver == null) throw new ArgumentNullException("messageReceiver");
            if(shouldAutoCompleteMessages && messageReceiver.Mode != ReceiveMode.PeekLock) throw new ArgumentException("Auto-completion of messages is only supported for ReceiveMode of PeekLock.", "shouldAutoCompleteMessages");

            Initialize(messageReceiver.WhenMessageReceived(), deserializers, shouldAutoCompleteMessages);
        }

        public MessageSource(IObservable<BrokeredMessage> brokeredMessages, IEnumerable<IMessageDeserializer<TMessage>> deserializers)
            : this(brokeredMessages, deserializers, false)
        {
        }

        public MessageSource(IObservable<BrokeredMessage> brokeredMessages, IEnumerable<IMessageDeserializer<TMessage>> deserializers, bool shouldAutoCompleteMessages)
        {
            Initialize(brokeredMessages, deserializers, shouldAutoCompleteMessages);
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

        public void Dispose()
        {
        }

        private void Initialize(IObservable<BrokeredMessage> brokeredMessages, IEnumerable<IMessageDeserializer<TMessage>> deserializers, bool shouldAutoCompleteMessages)
        {
            if(brokeredMessages == null) throw new ArgumentNullException("brokeredMessages");
            if(deserializers == null) throw new ArgumentNullException("deserializers");

            _brokeredMessages = brokeredMessages;
            _deserializers = deserializers.ToDictionary(d => d.GetTypeName());
            _shouldAutoCompleteMessages = shouldAutoCompleteMessages;
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
