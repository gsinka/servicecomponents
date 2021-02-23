using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Serilog.Context;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Infrastructure.Receiver
{
    public class QueryReceiverLogBehavior : ReceiverLogBehavior, IReceiveQuery
    {
        private readonly IReceiveQuery _next;

        public QueryReceiverLogBehavior(IOptions<CorrelationLogOptions> logOptions, IReceiveQuery next) : base(logOptions)
        {
            _next = next;
        }

        public async Task<TResult> ReceiveAsync<TResult>(IQuery<TResult> query, string correlationId = null, string causationId = null, string userId = null, string userName = null, CancellationToken cancellationToken = default)
        {
            if (query != null) LogContext.PushProperty(LogOptions.QueryIdPropertyName, query.QueryId);
            PushProperties(correlationId, causationId, userId, userName);
            return await _next.ReceiveAsync(query, correlationId, causationId, userId, userName, cancellationToken);
        }
    }
}