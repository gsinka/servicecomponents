using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application;

namespace ServiceComponents.Infrastructure.Senders.Loopback
{
    public interface IPublishLoopbackEvent
    {
        Task PublishAsync<TEvent>(TEvent @event, ICorrelation correlation, IDictionary<string, string> args = default, CancellationToken cancellationToken = default) where TEvent : IEvent;
        Task PublishAsync(IEnumerable<IEvent> events, ICorrelation correlation, IDictionary<string, string> args = default, CancellationToken cancellationToken = default);
    }
}