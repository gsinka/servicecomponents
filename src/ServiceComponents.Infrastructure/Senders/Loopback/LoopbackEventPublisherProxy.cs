using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application;
using ServiceComponents.Application.Senders;

namespace ServiceComponents.Infrastructure.Senders.Loopback
{
    public class LoopbackEventPublisherProxy : IPublishEvent
    {
        private readonly ILifetimeScope _scope;
        private readonly IPublishLoopbackEvent _next;

        public LoopbackEventPublisherProxy(ILifetimeScope scope, IPublishLoopbackEvent next)
        {
            _scope = scope;
            _next = next;
        }

        public async Task PublishAsync<TEvent>(TEvent @event, IDictionary<string, string> args = default, CancellationToken cancellationToken = default) where TEvent : IEvent
        {
            var correlation = _scope.ResolveOptional<ICorrelation>();
            await _next.PublishAsync(@event, correlation, args, cancellationToken);
        }

        public async Task PublishAsync(IEnumerable<IEvent> events, IDictionary<string, string> args = default, CancellationToken cancellationToken = default)
        {
            var correlation = _scope.ResolveOptional<ICorrelation>();
            await _next.PublishAsync(events, correlation, args, cancellationToken);
        }
    }
}