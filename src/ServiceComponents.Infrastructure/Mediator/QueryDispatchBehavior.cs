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
    public class QueryDispatchBehavior : IDispatchQuery
    {
        private readonly ILifetimeScope _scope;
        private readonly IDispatchQuery _next;

        public QueryDispatchBehavior(ILifetimeScope scope, IDispatchQuery next)
        {
            _scope = scope;
            _next = next;
        }

        public async Task<TResult> DispatchAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
        {
            try
            {
                // Pre

                foreach (var preHandler in _scope.Resolve<IEnumerable<IPreHandleQuery>>()) {
                    await preHandler.PreHandleAsync(query, cancellationToken);
                }

                foreach (dynamic preHandler in (IEnumerable<dynamic>)_scope.Resolve(typeof(IEnumerable<>).MakeGenericType(typeof(IPreHandleQuery<,>).MakeGenericType(query.GetType(), typeof(TResult))))) {
                    await preHandler.PreHandleAsync((dynamic)query, cancellationToken);
                }

                // Inner

                var result = await _next.DispatchAsync(query, cancellationToken);

                // Post

                foreach (var postHandler in _scope.Resolve<IEnumerable<IPostHandleQuery>>()) {
                    await postHandler.PostHandleAsync(query, result, cancellationToken);
                }

                foreach (dynamic postHandler in (IEnumerable<dynamic>)_scope.Resolve(typeof(IEnumerable<>).MakeGenericType(typeof(IPostHandleQuery<,>).MakeGenericType(query.GetType(), typeof(TResult))))) {
                    await postHandler.PostHandleAsync((dynamic)query, result, cancellationToken);
                }

                return result;
            }
            catch (Exception exception)
            {
                // Error

                foreach (var errorHandler in _scope.Resolve<IEnumerable<IHandleQueryFailure>>()) {
                    await errorHandler.HandleFailureAsync(query, exception, cancellationToken);
                }

                foreach (dynamic errorHandler in (IEnumerable<dynamic>)_scope.Resolve(typeof(IEnumerable<>).MakeGenericType(typeof(IHandleQueryFailure<,>).MakeGenericType(query.GetType(), typeof(TResult))))) {
                    await errorHandler.HandleFailureAsync((dynamic)query, exception, cancellationToken);
                }

                throw;
            }
        }
    }
}