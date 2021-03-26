using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application;

namespace ServiceComponents.Infrastructure.Receivers.Loopback
{
    public class LoopbackEventReceiver : IReceiveLoopbackEvent
    {
        private readonly IReceiveEvent _receiver;

        public LoopbackEventReceiver(IReceiveEvent receiver)
        {
            _receiver = receiver;
        }

        public async Task ReceiveAsync<T>(T @event, ICorrelation correlation, CancellationToken cancellationToken = default) where T : IEvent
        {
            await _receiver.ReceiveAsync(@event, cancellationToken);
        }
    }
}