using System.Threading;
using System.Threading.Tasks;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application;
using ServiceComponents.Infrastructure.CorrelationContext;

namespace ServiceComponents.Infrastructure.Receivers.Loopback
{
    public class LoopbackQueryReceiverCorrelationBehavior : LoopbackReceiverCorrelationBehavior, IReceiveLoopbackQuery
    {
        private readonly IReceiveLoopbackQuery _next;

        public LoopbackQueryReceiverCorrelationBehavior(ILogger log, IReceiveLoopbackQuery next, Correlation correlation) : base(log, correlation)
        {
            _next = next;
        }

        public async Task<TResult> ReceiveAsync<TResult>(IQuery<TResult> query, ICorrelation correlation, CancellationToken cancellationToken = default)
        {
            SetCorrelation(query.QueryId, correlation);
            return await _next.ReceiveAsync(query, correlation, cancellationToken);
        }
    }
}