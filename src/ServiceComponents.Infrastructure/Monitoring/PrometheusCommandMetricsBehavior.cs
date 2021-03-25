using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Infrastructure.Receivers;

namespace ServiceComponents.Infrastructure.Monitoring
{
    public class PrometheusCommandMetricsBehavior : IReceiveCommand
    {
        private readonly IMetricsService _metrics;
        private readonly IReceiveCommand _next;

        public PrometheusCommandMetricsBehavior(IMetricsService metrics, IReceiveCommand next)
        {
            _metrics = metrics;
            _next = next;
        }

        public async Task ReceiveAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand
        {
            var stopWatch = new Stopwatch();

            _metrics.Increment(new RequestCounterMetric(command));

            try {

                stopWatch.Start();
                await _next.ReceiveAsync(command, cancellationToken);
                stopWatch.Stop();

                _metrics.Observe(new RequestDurationMetric(command), stopWatch.ElapsedMilliseconds);
            }
            catch (Exception exception) {

                stopWatch.Stop();
                _metrics.Increment(new RequestFailureMetric(command, exception));

                throw;
            }
        }
    }
}