using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Serilog.Context;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Infrastructure.Receivers;

namespace ServiceComponents.Infrastructure.Behaviors.Logging
{
    public class EventReceiverCorrelationEnricherBehavior : ReceiverCorrelationBehavior, IReceiveEvent
    {
        private readonly IReceiveEvent _next;

        public EventReceiverCorrelationEnricherBehavior(IOptions<CorrelationLogOptions> options, CorrelationContext.Correlation correlation, IReceiveEvent next)
            : base(options, correlation)
        {
            _next = next;
        }
        
        public async Task ReceiveAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IEvent
        {
            Enrich(Options.EventIdPropertyName, @event.EventId);
            await _next.ReceiveAsync(@event, cancellationToken);
        }
    }
}