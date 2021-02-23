using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Infrastructure.CorrelationContext;

namespace ServiceComponents.Infrastructure.Rabbit
{
    public class RabbitEventReceiverCorrelationBehavior : RabbitReceiverCorrelationBehavior, IReceiveRabbitEvent
    {
        private readonly IReceiveRabbitEvent _next;

        public RabbitEventReceiverCorrelationBehavior(ILogger log, Correlation correlation, IReceiveRabbitEvent next) : base(log, correlation)
        {
            _next = next;
        }

        public async Task ReceiveAsync<T>(T @event, BasicDeliverEventArgs args, CancellationToken cancellationToken) where T : IEvent
        {
            UpdateCorrelation(args, @event.EventId);
            await _next.ReceiveAsync(@event, args, cancellationToken);
        }
    }
}