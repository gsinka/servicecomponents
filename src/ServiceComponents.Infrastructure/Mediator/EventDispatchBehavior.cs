using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Mediator;
using ServiceComponents.Infrastructure.Dispatchers;

namespace ServiceComponents.Infrastructure.Mediator
{
    public class EventDispatchBehavior : IDispatchEvent
    {
        private readonly ILifetimeScope _scope;
        private readonly IDispatchEvent _next;

        public EventDispatchBehavior(ILifetimeScope scope, IDispatchEvent next)
        {
            _scope = scope;
            _next = next;
        }

        public async Task DispatchAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IEvent
        {
            try
            {
                // Pre

                foreach (var preHandler in _scope.Resolve<IEnumerable<IPreHandleEvent>>()) {
                    await preHandler.PreHandleAsync(@event, cancellationToken);
                }

                foreach (dynamic preHandler in (IEnumerable<dynamic>)_scope.Resolve(typeof(IEnumerable<>).MakeGenericType(typeof(IPreHandleEvent<>).MakeGenericType(@event.GetType())))) {
                    await preHandler.PreHandleAsync((dynamic)@event, cancellationToken);
                }

                // Inner

                await _next.DispatchAsync(@event, cancellationToken);

                // Post

                foreach (var postHandler in _scope.Resolve<IEnumerable<IPostHandleEvent>>()) {
                    await postHandler.PostHandleAsync(@event, cancellationToken);
                }

                foreach (dynamic postHandler in (IEnumerable<dynamic>)_scope.Resolve(typeof(IEnumerable<>).MakeGenericType(typeof(IPostHandleEvent<>).MakeGenericType(@event.GetType())))) {
                    await postHandler.PostHandleAsync((dynamic)@event, cancellationToken);
                }
            }
            catch (Exception exception)
            {
                // Error

                foreach (var errorHandler in _scope.Resolve<IEnumerable<IHandleEventFailure>>()) {
                    await errorHandler.HandleFailureAsync(@event, exception, cancellationToken);
                }

                foreach (dynamic errorHandler in (IEnumerable<dynamic>)_scope.Resolve(typeof(IEnumerable<>).MakeGenericType(typeof(IHandleEventFailure<>).MakeGenericType(@event.GetType())))) {
                    await errorHandler.HandleFailureAsync((dynamic)@event, exception, cancellationToken);
                }

                throw;
            }
        }
    }
}