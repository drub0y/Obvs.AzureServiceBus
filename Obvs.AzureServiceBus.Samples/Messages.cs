using Obvs.Types;
using Obvs.AzureServiceBus;

namespace Obvs.AzureServiceBus.Samples
{
    public class SampleMessage : PeekLockMessage
    {
    }


    public class SampleCommand : SampleMessage, ICommand
    {
        public string CommandId
        {
            get;
            set;
        }
    }

    public class SampleEvent : SampleMessage, IEvent
    {
        public string EventId
        {
            get;
            set;
        }
    }

    public class SampleRequest : SampleMessage, IRequest
    {
        public string RequestId
        {
            get;
            set;
        }

        public string RequesterId
        {
            get;
            set;
        }
    }

    public class SampleResponse : SampleMessage, IResponse
    {
        public string RequestId
        {
            get;
            set;
        }

        public string RequesterId
        {
            get;
            set;
        }
    }
}
