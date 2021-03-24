using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Mediator;
using ServiceComponents.Core.Extensions;
using TypeExtensions = ServiceComponents.Core.Extensions.TypeExtensions;

namespace ServiceComponents.Infrastructure.Dispatchers
{
    public class EventDispatcher : IDispatchEvent
    {
        private readonly ILogger _log;
        private readonly ILifetimeScope _scope;

        public EventDispatcher(ILogger log, ILifetimeScope scope)
        {
            _log = log;
            _scope = scope;
        }

        public async Task DispatchAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IEvent
        {
            var handlers = (IEnumerable<dynamic>)_scope.ResolveOptional(typeof(IEnumerable<>).MakeGenericType(typeof(IHandleEvent<>).MakeGenericType(@event.GetType())));

            foreach (dynamic handler in handlers) {
                _log.Verbose("Dispatching {eventType} to {handlerType}", @event.DisplayName(), TypeExtensions.DisplayName(handler));
                await handler.HandleAsync((dynamic)@event, cancellationToken);
            }
            
        }
    }
}