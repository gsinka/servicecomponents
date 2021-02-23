using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Infrastructure.Dispatchers;

namespace ServiceComponents.Infrastructure.Receivers
{
    public class EventReceiver : IReceiveEvent
    {
        private readonly IDispatchEvent _eventDispatcher;

        public EventReceiver(IDispatchEvent eventDispatcher)
        {
            _eventDispatcher = eventDispatcher;
        }

        public async Task ReceiveAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IEvent
        {
            await _eventDispatcher.DispatchAsync(@event, cancellationToken);
        }
    }
}