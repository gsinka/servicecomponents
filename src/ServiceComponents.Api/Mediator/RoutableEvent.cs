namespace ServiceComponents.Api.Mediator
{
    public class RoutableEvent : Event
    {
        public IEvent Event { get; }
        public string RoutingKey { get; }

        public RoutableEvent(IEvent @event, string routingKey) : base(@event.EventId)
        {
            Event = @event;
            RoutingKey = routingKey;
        }
    }
}