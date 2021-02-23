using System.Threading;
using System.Threading.Tasks;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Core.ExtensionMethods;
using ServiceComponents.Infrastructure.Receivers;

namespace ServiceComponents.Infrastructure.Http
{
    public class HttpEventReceiver : IReceiveHttpEvent
    {
        private readonly ILogger _log;
        private readonly IReceiveEvent _eventReceiver;

        public HttpEventReceiver(ILogger log, IReceiveEvent eventReceiver)
        {
            _log = log;
            _eventReceiver = eventReceiver;
        }

        public async Task ReceiveAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent
        {
            _log.Verbose("Receiving {eventType} on http", @event.DisplayName());
            await _eventReceiver.ReceiveAsync(@event, cancellationToken);
        }
    }
}