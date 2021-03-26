using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Monitoring;
using ServiceComponents.Infrastructure.Receivers;

namespace ServiceComponents.Infrastructure.Monitoring
{
    public class PrometheusQueryMetricsBehavior : IReceiveQuery
    {
        private readonly IMetricsService _metrics;
        private readonly IReceiveQuery _next;

        public PrometheusQueryMetricsBehavior(IMetricsService metrics, IReceiveQuery next)
        {
            _metrics = metrics;
            _next = next;
        }

        public async Task<TResult> ReceiveAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
        {
            var stopWatch = new Stopwatch();

            _metrics.Increment(new RequestCounterMetric(query));
            _metrics.Increment(new RequestGaugeMetric("generic_request_gauge", "Current request count"));
            try {

                stopWatch.Start();
                var result = await _next.ReceiveAsync(query, cancellationToken);
                stopWatch.Stop();

                _metrics.Observe(new RequestDurationMetric(query), stopWatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception exception) {

                stopWatch.Stop();
                _metrics.Increment(new RequestFailureMetric(query, exception));

                throw;
            }
            finally {
                _metrics.Decrement(new RequestGaugeMetric("generic_request_gauge", "Current request count"));
            }
        }
    }
}