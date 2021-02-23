using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Infrastructure.Receivers;

namespace ServiceComponents.Infrastructure.Rabbit
{
    public class RabbitEventReceiver : IReceiveRabbitEvent
    {
        private readonly IReceiveEvent _eventReceiver;

        public RabbitEventReceiver(IReceiveEvent eventReceiver)
        {
            _eventReceiver = eventReceiver;
        }

        public async Task ReceiveAsync<T>(T @event, BasicDeliverEventArgs args, CancellationToken cancellationToken) where T : IEvent
        {
            await _eventReceiver.ReceiveAsync(@event, cancellationToken);
        }
    }
}
