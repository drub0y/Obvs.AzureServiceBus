using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.ServiceBus.Messaging;
using Obvs.Types;

namespace Obvs.AzureServiceBus
{
    public class MessageSource<TMessage> : IMessageSource<TMessage> 
        where TMessage : IMessage
    {
        private MessageReceiver _messageReceiver;
        private Dictionary<string, IMessageDeserializer<TMessage>> _deserializers;
        

        public MessageSource(MessageReceiver messageReceiver, IEnumerable<IMessageDeserializer<TMessage>> deserializers)
        {
            if(messageReceiver == null) throw new ArgumentNullException("messageReceiver");
            if(deserializers == null) throw new ArgumentNullException("deserializers");
            
            _messageReceiver = messageReceiver;
            _deserializers = deserializers.ToDictionary(d => d.GetTypeName());
        }

        public IObservable<TMessage> Messages
        {
            get
            {
                return Observable.Create<TMessage>(async o =>
                    {
                        CancellationDisposable cancellationDispoable = new CancellationDisposable();

                        do
                        {
                            using(BrokeredMessage brokeredMessage = await _messageReceiver.ReceiveAsync())
                            {
                                TMessage message = Deserialize(brokeredMessage);

                                try
                                {
                                    o.OnNext(default(TMessage));

                                    await brokeredMessage.CompleteAsync();
                                }
                                catch(Exception exception)
                                {
                                    o.OnError(exception);
                                }
                            }
                        } while(!cancellationDispoable.IsDisposed);

                        o.OnCompleted();

                        return cancellationDispoable;
                    });
            }
        }

        public void Dispose()
        {
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
