using System.Collections.Generic;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Domain
{
    public interface IAggregateRoot
    {
        string AggregateId { get; }

        long Version { get; }
    }
}
