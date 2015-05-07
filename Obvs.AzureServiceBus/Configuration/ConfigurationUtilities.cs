using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ServiceBus.Messaging;

namespace Obvs.AzureServiceBus.Configuration
{
    internal static class MessageReceiveModeTranslator
    {
        public static ReceiveMode TranslateReceiveModeConfigurationValueToAzureServiceBusValue(MessageReceiveMode messageReceiveMode)
        {
            ReceiveMode result;

            switch(messageReceiveMode)
            {
                case MessageReceiveMode.PeekLock:
                    result = Microsoft.ServiceBus.Messaging.ReceiveMode.PeekLock;

                    break;

                case MessageReceiveMode.ReceiveAndDelete:
                    result = Microsoft.ServiceBus.Messaging.ReceiveMode.ReceiveAndDelete;

                    break;

                default:
                    throw new ArgumentOutOfRangeException("configurationReceiveMode", "Unexpected MessageReceiveMode value specified: " + messageReceiveMode.ToString());
            }

            return result;
        }

        public static MessageReceiveMode TranslateAzureServiceBusReceiveModeValueToConfigurationValue(Microsoft.ServiceBus.Messaging.ReceiveMode azureServiceBusReceiveMode)
        {
            MessageReceiveMode result;

            switch(azureServiceBusReceiveMode)
            {
                case Microsoft.ServiceBus.Messaging.ReceiveMode.PeekLock:
                    result = MessageReceiveMode.PeekLock;

                    break;

                case Microsoft.ServiceBus.Messaging.ReceiveMode.ReceiveAndDelete:
                    result = MessageReceiveMode.ReceiveAndDelete;

                    break;

                default:
                    throw new ArgumentOutOfRangeException("azureServiceBusReceiveMode", "Unexpected ReceiveMode value specified: " + azureServiceBusReceiveMode.ToString());
            }

            return result;
        }
    }
}
