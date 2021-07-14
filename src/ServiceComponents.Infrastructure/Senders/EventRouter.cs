using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Senders;
using ServiceComponents.Core.Extensions;

namespace ServiceComponents.Infrastructure.Senders
{
    public class EventRouter : IPublishEvent
    {
        private readonly ILogger _log;
        private readonly ILifetimeScope _scope;
        private readonly Func<IEvent, object> _keySelector;

        public EventRouter(ILogger log, ILifetimeScope scope, Func<IEvent, object> keySelector)
        {
            _log = log;
            _scope = scope;
            _keySelector = keySelector;
        }

        public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent
        {
            var key = _keySelector(@event);
            var sender = _scope.ResolveKeyed<IPublishEvent>(key);

            _log.Verbose("Routing {commandType} to {senderType} based on key '{routingKey}'", @event.DisplayName(), sender.DisplayName(), key);
            await sender.PublishAsync(@event, cancellationToken);
        }

        public async Task PublishAsync(IEnumerable<IEvent> events, CancellationToken cancellationToken = default)
        {
            foreach (IEvent @event in events) {
                await PublishAsync(@event, cancellationToken);
            }
        }
    }
}