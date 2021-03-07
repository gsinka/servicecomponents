using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Infrastructure.CorrelationContext;

namespace ServiceComponents.Infrastructure.Receivers
{
    public class LoopbackEventReceiverCorrelationBehavior : LoopbackReceiverCorrelationBehavior, IReceiveLoopbackEvent
    {
        private readonly IReceiveLoopbackEvent _next;
        private readonly Correlation _correlation;

        public LoopbackEventReceiverCorrelationBehavior(IReceiveLoopbackEvent next, Correlation correlation) : base(correlation)
        {
            _next = next;
            _correlation = correlation;
        }

        public async Task ReceiveAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IEvent
        {
            SetCorrelation(@event.EventId);
            await _next.ReceiveAsync(@event, cancellationToken);
            ResetCorrelation();
        }
    }
}