using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Monitoring;
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
            _metrics.Increment(new RequestCounterMetric(command));
            _metrics.Increment(new RequestGaugeMetric("generic_request_gauge", "Current request count"));

            try {

                using (_metrics.Observe(new RequestDurationMetric(command)))
                {
                    await _next.ReceiveAsync(command, cancellationToken);
                }
            }
            catch (Exception exception) {
                
                _metrics.Increment(new RequestFailureMetric(command, exception));
                throw;
            }
            finally {
                _metrics.Decrement(new RequestGaugeMetric("generic_request_gauge", "Current request count"));
            }
        }
    }
}