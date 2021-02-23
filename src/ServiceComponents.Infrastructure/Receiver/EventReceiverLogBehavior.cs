using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Serilog.Context;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Infrastructure.Receiver
{
    public class EventReceiverLogBehavior : ReceiverLogBehavior, IReceiveEvent
    {
        private readonly IReceiveEvent _next;

        public EventReceiverLogBehavior(IOptions<CorrelationLogOptions> logOptions, IReceiveEvent next) : base(logOptions)
        {
            _next = next;
        }

        public async Task ReceiveAsync(IEvent @event, string correlationId = null, string causationId = null, string userId = null, string userName = null, CancellationToken cancellationToken = default)
        {
            if (@event != null) LogContext.PushProperty(LogOptions.EventIdPropertyName, @event.EventId);
            PushProperties(correlationId, causationId, userId, userName);
            await _next.ReceiveAsync(@event, correlationId, causationId, userId, userName, cancellationToken);
        }
    }
}