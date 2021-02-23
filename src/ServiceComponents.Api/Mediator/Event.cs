using System;

namespace ServiceComponents.Api.Mediator
{
    public abstract class Event : IEvent
    {
        public string EventId { get; }

        protected Event(string eventId = null)
        {
            EventId = eventId ?? Guid.NewGuid().ToString();
        }
    }
}