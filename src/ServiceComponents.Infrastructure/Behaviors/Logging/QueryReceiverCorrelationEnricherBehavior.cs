using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Infrastructure.Receivers;

namespace ServiceComponents.Infrastructure.Behaviors.Logging
{
    public class QueryReceiverCorrelationEnricherBehavior : ReceiverCorrelationBehavior, IReceiveQuery
    {
        private readonly IReceiveQuery _next;

        public QueryReceiverCorrelationEnricherBehavior(IOptions<CorrelationLogOptions> options, CorrelationContext.Correlation correlation, IReceiveQuery next) 
            : base(options, correlation)
        {
            _next = next;
        }

        public async Task<TResult> ReceiveAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
        {
            Enrich(Options.QueryIdPropertyName, query.QueryId);
            return await _next.ReceiveAsync(query, cancellationToken);
        }
    }
}