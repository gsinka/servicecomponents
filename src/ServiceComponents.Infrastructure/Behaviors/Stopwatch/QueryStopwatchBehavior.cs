using System.Threading;
using System.Threading.Tasks;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Infrastructure.Dispatchers;
using ServiceComponents.Infrastructure.Mediator;

namespace ServiceComponents.Infrastructure.Behaviors.Stopwatch
{
    public class QueryStopwatchBehavior : IDispatchQuery
    {
        private readonly ILogger _log;
        private readonly IDispatchQuery _next;

        public QueryStopwatchBehavior(ILogger log, IDispatchQuery next)
        {
            _log = log;
            _next = next;
        }

        public async Task<TResult> DispatchAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
        {
            var stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();

            try
            {
                var result = await _next.DispatchAsync(query, cancellationToken);
                stopWatch.Stop();
                _log.Verbose("{query} processed in {duration} ms", query.GetType().Name, stopWatch.ElapsedMilliseconds);
                return result;
            }
            catch
            {
                stopWatch.Stop();
                _log.Verbose("{query} failed in {duration} ms", query.GetType().Name, stopWatch.ElapsedMilliseconds);
                throw;
            }

        }
    }
}