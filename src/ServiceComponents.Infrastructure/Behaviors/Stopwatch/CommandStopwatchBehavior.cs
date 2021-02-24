using System.Threading;
using System.Threading.Tasks;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Mediator;
using ServiceComponents.Infrastructure.Dispatchers;

namespace ServiceComponents.Infrastructure.Behaviors.Stopwatch
{
    public class CommandStopwatchBehavior : IDispatchCommand
    {
        private readonly ILogger _log;
        private readonly IDispatchCommand _next;

        public CommandStopwatchBehavior(ILogger log, IDispatchCommand next)
        {
            _log = log;
            _next = next;
        }

        public async Task DispatchAsync<T>(T command, CancellationToken cancellationToken = default) where T : ICommand
        {
            var stopWatch = new System.Diagnostics.Stopwatch();
            stopWatch.Start();

            try
            {
                await _next.DispatchAsync(command, cancellationToken);
                stopWatch.Stop();
                _log.Verbose("{command} processed in {duration} ms", command.GetType().Name, stopWatch.ElapsedMilliseconds);
            }
            catch
            {
                stopWatch.Stop();
                _log.Verbose("{command} failed in {duration} ms", command.GetType().Name, stopWatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
