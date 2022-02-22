using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Domain
{
    public abstract class EventSourcedAggregateRoot : IEventSourceAggregateRoot
    {
        public string AggregateId { get; protected set; }
        public long Version { get; protected set; }

        private readonly ICollection<IEvent> _uncommittedEvents = new List<IEvent>();

        protected EventSourcedAggregateRoot(string aggregateId)
        {
            if (string.IsNullOrEmpty(aggregateId)) throw new ArgumentNullException(nameof(aggregateId));
            AggregateId = aggregateId;
        }

        protected EventSourcedAggregateRoot(IEnumerable<IEvent> events)
        {
            if (events == null) throw new ArgumentException(nameof(events));
            foreach (IEvent @event in events) { ApplyEvent(@event); }
        }

        protected void RaiseEvent(IEvent @event)
        {
            _uncommittedEvents.Add(@event);
            ApplyEvent(@event);
        }

        protected void ApplyEvent(IEvent @event)
        {
            Version++;
            
            var handler = GetType()
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
                .SingleOrDefault(info => info.Name == "Apply" && info.GetParameters().Length == 1 && Enumerable.Single<ParameterInfo>(info.GetParameters()).ParameterType == @event.GetType());
            
            if (handler == null) {
                throw new InvalidOperationException($"No handler for event {@event.GetType().FullName} found on aggregate {GetType().FullName}");
            }

            handler.Invoke(this, new []{ @event});
        }

        ICollection<IEvent> IEventSourceAggregateRoot.GetUncommittedEvents() => _uncommittedEvents;
        void IEventSourceAggregateRoot.ClearUncommittedEvents() => _uncommittedEvents.Clear();
    }
}