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

                foreach (var preHandler in _scope.Resolve<IEnumerable<IPreHandleQuery>>())
                {
                    await preHandler.PreHandleAsync(query, cancellationToken);
                }

                foreach (var preHandler in _scope.Resolve<IEnumerable<IPreHandleQuery<IQuery<TResult>, TResult>>>())
                {
                    await preHandler.PreHandleAsync(query, cancellationToken);
                }

                // Inner

                var result = await _next.DispatchAsync(query, cancellationToken);

                // Post

                foreach (var postHandler in _scope.Resolve<IEnumerable<IPostHandleQuery>>())
                {
                    await postHandler.PostHandleAsync(query, result, cancellationToken);
                }

                foreach (var postHandler in _scope.Resolve<IEnumerable<IPostHandleQuery<IQuery<TResult>, TResult>>>())
                {
                    await postHandler.PostHandleAsync(query, result, cancellationToken);
                }

                return result;
            }
            catch (Exception exception)
            {
                // Error

                foreach (var errorHandler in _scope.Resolve<IEnumerable<IHandleQueryFailure>>())
                {
                    await errorHandler.HandleFailureAsync(query, exception, cancellationToken);
                }

                foreach (var errorHandler in _scope.Resolve<IEnumerable<IHandleQueryFailure<IQuery<TResult>, TResult>>>())
                {
                    await errorHandler.HandleFailureAsync(query, exception, cancellationToken);
                }

                throw;
            }
        }
    }
}