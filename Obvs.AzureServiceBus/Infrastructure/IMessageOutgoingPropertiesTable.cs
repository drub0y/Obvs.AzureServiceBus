using System;
using System.Runtime.CompilerServices;

namespace Obvs.AzureServiceBus.Infrastructure
{
    public interface IMessageOutgoingPropertiesTable
    {
        void SetOutgoingPropertiesForMessage(object message, IOutgoingMessageProperties outgoingMessageProperties);

        IOutgoingMessageProperties GetOutgoingPropertiesForMessage(object message);

        void RemoveOutgoingPropertiesForMessage(object message);
    }

    internal sealed class MessageOutgoingPropertiesTable
    {
        private static readonly IMessageOutgoingPropertiesTable Instance = new DefaultMessageOutgoingPropertiesTable();

        public static IMessageOutgoingPropertiesTable ConfiguredInstance
        {
            get
            {
                return Instance;
            }
        }
    }

    internal sealed class DefaultMessageOutgoingPropertiesTable : IMessageOutgoingPropertiesTable
    {
        ConditionalWeakTable<object, IOutgoingMessageProperties> _innerTable = new ConditionalWeakTable<object, IOutgoingMessageProperties>();

        public void SetOutgoingPropertiesForMessage(object message, IOutgoingMessageProperties outgoingMessageProperties)
        {
            if(message == null) throw new ArgumentNullException(nameof(message));
            if(outgoingMessageProperties == null) throw new ArgumentNullException(nameof(outgoingMessageProperties));

            _innerTable.Add(message, outgoingMessageProperties);
        }

        public IOutgoingMessageProperties GetOutgoingPropertiesForMessage(object message)
        {
            IOutgoingMessageProperties result;

            _innerTable.TryGetValue(message, out result);

            return result;
        }

        public void RemoveOutgoingPropertiesForMessage(object message) => _innerTable.Remove(message);
    }
}
