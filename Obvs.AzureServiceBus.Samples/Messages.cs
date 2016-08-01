using Obvs.Types;
using Obvs.AzureServiceBus;

namespace Obvs.AzureServiceBus.Samples
{
    public class SampleMessage
    {
    }


    public class SampleCommand : SampleMessage
    {
        public string CommandId
        {
            get;
            set;
        }
    }

    public class SampleEvent : SampleMessage
    {
        public string EventId
        {
            get;
            set;
        }
    }

    public class SampleRequest : SampleMessage
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

    public class SampleResponse : SampleMessage
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
