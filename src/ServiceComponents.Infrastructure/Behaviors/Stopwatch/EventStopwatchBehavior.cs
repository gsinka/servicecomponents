using System.Threading;
using System.Threading.Tasks;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Infrastructure.Dispatchers;
using ServiceComponents.Infrastructure.Mediator;

namespace ServiceComponents.Infrastructure.Behaviors.Stopwatch
{
    public class EventStopwatchBehavior : IDispatchEvent
    {
        private readonly ILogger _log;
        private readonly IDispatchEvent _next;

        public EventStopwatchBehavior(ILogger log, IDispatchEvent next)
        {
            _log = log;
            _next = next;
        }

        public async Task DispatchAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IEvent
        {
            var stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();

            try
            {
                await _next.DispatchAsync(@event, cancellationToken);
                stopWatch.Stop();
                _log.Verbose("{event} processed in {duration} ms", @event.GetType().Name, stopWatch.ElapsedMilliseconds);
            }
            catch
            {
                stopWatch.Stop();
                _log.Verbose("{event} failed in {duration} ms", @event.GetType().Name, stopWatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
