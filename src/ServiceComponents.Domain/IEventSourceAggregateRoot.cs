using System.Collections.Generic;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Domain
{
    public interface IEventSourceAggregateRoot : IAggregateRoot
    {
        ICollection<IEvent> GetUncommittedEvents();
        void ClearUncommittedEvents();
    }
}