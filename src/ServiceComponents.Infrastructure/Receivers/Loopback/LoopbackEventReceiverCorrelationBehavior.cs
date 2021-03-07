using System.Threading;
using System.Threading.Tasks;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application;
using ServiceComponents.Infrastructure.CorrelationContext;

namespace ServiceComponents.Infrastructure.Receivers
{
    public class LoopbackEventReceiverCorrelationBehavior : LoopbackReceiverCorrelationBehavior, IReceiveLoopbackEvent
    {
        private readonly IReceiveLoopbackEvent _next;

        public LoopbackEventReceiverCorrelationBehavior(ILogger log, IReceiveLoopbackEvent next, Correlation correlation) : base(log, correlation)
        {
            _next = next;
        }

        public async Task ReceiveAsync<T>(T @event, ICorrelation correlation, CancellationToken cancellationToken = default) where T : IEvent
        {
            SetCorrelation(@event.EventId, correlation);
            await _next.ReceiveAsync(@event, correlation, cancellationToken);
        }
    }
}