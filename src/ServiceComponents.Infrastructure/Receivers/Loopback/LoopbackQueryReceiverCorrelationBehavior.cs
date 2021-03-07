using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Infrastructure.CorrelationContext;

namespace ServiceComponents.Infrastructure.Receivers
{
    public class LoopbackQueryReceiverCorrelationBehavior : LoopbackReceiverCorrelationBehavior, IReceiveLoopbackQuery
    {
        private readonly IReceiveLoopbackQuery _next;
        private readonly Correlation _correlation;

        public LoopbackQueryReceiverCorrelationBehavior(IReceiveLoopbackQuery next, Correlation correlation) : base(correlation)
        {
            _next = next;
            _correlation = correlation;
        }

        public async Task<TResult> ReceiveAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
        {
            SetCorrelation(query.QueryId);
            var result = await _next.ReceiveAsync(query, cancellationToken);
            ResetCorrelation();

            return result;
        }
    }
}