using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Obvs.Types;

namespace Obvs.AzureServiceBus.Samples
{
    public interface ISampleMessage : IMessage
    {
    }

    public class SampleCommand : ISampleMessage, ICommand
    {
        public string CommandId
        {
            get;
            set;
        }
    }

    public class SampleEvent : ISampleMessage, IEvent
    {
    }

    public class SampleRequest : ISampleMessage, IRequest
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

    public class SampleResponse : ISampleMessage, IResponse
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
