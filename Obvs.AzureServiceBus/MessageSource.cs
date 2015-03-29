using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.ServiceBus.Messaging;
using Obvs.Types;
using HackedBrain.WindowsAzure.ServiceBus.Messaging;

namespace Obvs.AzureServiceBus
{
    public class MessageSource<TMessage> : IMessageSource<TMessage> 
        where TMessage : IMessage
    {
        private IObservable<BrokeredMessage> _brokeredMessages;
        private Dictionary<string, IMessageDeserializer<TMessage>> _deserializers;        

        public MessageSource(MessageReceiver messageReceiver, IEnumerable<IMessageDeserializer<TMessage>> deserializers)
        {
            if(messageReceiver == null) throw new ArgumentNullException("messageReceiver");
            
            Initialize(messageReceiver.WhenMessageReceived(), deserializers);
        }

        public MessageSource(IObservable<BrokeredMessage> brokeredMessages, IEnumerable<IMessageDeserializer<TMessage>> deserializers)
        {
            Initialize(brokeredMessages, deserializers);
        }

        public IObservable<TMessage> Messages
        {
            get
            {
                return _brokeredMessages.Select(this.Deserialize);
            }
        }

        public void Dispose()
        {
        }

        private void Initialize(IObservable<BrokeredMessage> brokeredMessages, IEnumerable<IMessageDeserializer<TMessage>> deserializers)
        {
            if(brokeredMessages == null) throw new ArgumentNullException("brokeredMessages");
            if(deserializers == null) throw new ArgumentNullException("deserializers");

            _brokeredMessages = brokeredMessages;
            _deserializers = deserializers.ToDictionary(d => d.GetTypeName());
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
    }
}
