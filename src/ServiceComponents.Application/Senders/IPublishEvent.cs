using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Application.Senders
{
    public interface IPublishEvent
    {
        Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent;
        //Task PublishAsync<TEvent>(TEvent @event, IDictionary<string, string> args = default, CancellationToken cancellationToken = default) where TEvent : IEvent;

        Task PublishAsync(IEnumerable<IEvent> events, CancellationToken cancellationToken = default);
        //Task PublishAsync(IEnumerable<IEvent> events, IDictionary<string, string> args = default, CancellationToken cancellationToken = default);
    }
}
