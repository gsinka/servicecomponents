using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Monitoring;
using ServiceComponents.Infrastructure.Receivers;

namespace ServiceComponents.Infrastructure.Monitoring
{
    public class PrometheusEventMetricsBehavior : IReceiveEvent
    {
        private readonly IMetricsService _metrics;
        private readonly IReceiveEvent _next;

        public PrometheusEventMetricsBehavior(IMetricsService metrics, IReceiveEvent next)
        {
            _metrics = metrics;
            _next = next;
        }

        public async Task ReceiveAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IEvent
        {
            var stopWatch = new Stopwatch();

            _metrics.Increment(new RequestCounterMetric(@event));

            try {

                stopWatch.Start();
                await _next.ReceiveAsync(@event, cancellationToken);
                stopWatch.Stop();

                _metrics.Observe(new RequestDurationMetric(@event), stopWatch.ElapsedMilliseconds);
            }
            catch (Exception exception) {

                stopWatch.Stop();
                _metrics.Increment(new RequestFailureMetric(@event, exception));

                throw;
            }
        }
    }
}