using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application;
using ServiceComponents.Application.Mediator;

namespace ServiceComponents.Infrastructure.EventRecorder
{
    public class EventRecorderFeeder : IPreHandleEvent
    {
        private readonly EventRecorderService _eventRecorder;
        private readonly ICorrelation _correlation;

        public EventRecorderFeeder(EventRecorderService eventRecorder, ICorrelation correlation)
        {
            _eventRecorder = eventRecorder;
            _correlation = correlation;
        }

        public async Task PreHandleAsync(IEvent @event, CancellationToken cancellationToken = default)
        {
            await _eventRecorder.PreHandleEvent(@event, _correlation, cancellationToken);
        }
    }
}