using System;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application;

namespace ServiceComponents.AspNet.EventRecorder
{
    public class RecordedEvent
    {
        public DateTime TimeStamp { get; }
        public IEvent Event { get; }
        public ICorrelation Correlation { get; }

        public RecordedEvent(DateTime timeStamp, IEvent @event, ICorrelation correlation)
        {
            TimeStamp = timeStamp;
            Event = @event;
            Correlation = correlation;
        }
    }
}