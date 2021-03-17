using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Senders;
using ServiceComponents.Core.Extensions;

namespace ServiceComponents.Infrastructure.Senders
{
    public class QueryRouter : ISendQuery
    {
        private readonly ILogger _log;
        private readonly ILifetimeScope _scope;
        private readonly Func<IQuery, object> _keySelector;

        public QueryRouter(ILogger log, ILifetimeScope scope, Func<IQuery, object> keySelector)
        {
            _log = log;
            _scope = scope;
            _keySelector = keySelector;
        }

        public async Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
        {
            var key = _keySelector(query);
            var sender = _scope.ResolveKeyed<ISendQuery>(key);

            _log.Verbose("Routing {queryType} to {senderType} based on key '{routingKey}'", query.DisplayName(), sender.DisplayName(), key);
            return await sender.SendAsync(query, cancellationToken);
        }
    }
}