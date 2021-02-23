using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Mediator;
using ServiceComponents.Core.ExtensionMethods;
using TypeExtensions = ServiceComponents.Core.ExtensionMethods.TypeExtensions;

namespace ServiceComponents.Infrastructure.Dispatchers
{
    public class QueryDispatcher : IDispatchQuery
    {
        private readonly ILogger _log;
        private readonly ILifetimeScope _scope;

        public QueryDispatcher(ILogger log, ILifetimeScope scope)
        {
            _log = log;
            _scope = scope;
        }

        public async Task<TResult> DispatchAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
        {
            dynamic handler = _scope.ResolveOptional(typeof(IHandleQuery<,>).MakeGenericType(query.GetType(), typeof(TResult))) ?? throw new InvalidOperationException($"No handler found for {query.DisplayName()}");
            _log.Verbose("Dispatching {eventType} to {handlerType}", query.DisplayName(), TypeExtensions.DisplayName(handler));
            return await handler.HandleAsync((dynamic)query, cancellationToken);
        }
    }
}