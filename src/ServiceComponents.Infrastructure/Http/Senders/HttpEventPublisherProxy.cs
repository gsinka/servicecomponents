using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Senders;

namespace ServiceComponents.Infrastructure.Http.Senders
{
    public class HttpEventPublisherProxy : IPublishEvent
    {
        private readonly IPublishHttpEvent _next;

        public HttpEventPublisherProxy(IPublishHttpEvent next)
        {
            _next = next;
        }

        public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent
        {
            await _next.PublishAsync(@event, new Dictionary<string, string>(), cancellationToken);
        }

        public async Task PublishAsync(IEnumerable<IEvent> events, CancellationToken cancellationToken = default)
        {
            foreach (var @event in events)
            {
                await PublishAsync(@event, cancellationToken);
            }
        }
    }
}