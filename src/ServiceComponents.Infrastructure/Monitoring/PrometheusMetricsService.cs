using System.Collections.Generic;
using System.Linq;
using Prometheus;

namespace ServiceComponents.Infrastructure.Monitoring
{
    public class PrometheusMetricsService : IMetricsService
    {
        private readonly Dictionary<object, Counter> _counters = new();
        private readonly Dictionary<object, Summary> _summaries = new();

        public void Increment(object metric, double increment = 1)
        {
            if (!_counters.ContainsKey(metric.GetType())) {

                var (name, title) = metric.MetricDescription();
                _counters.Add(metric.GetType(), Metrics.CreateCounter(name, title, new CounterConfiguration() { LabelNames = metric.MetricFields().ToArray() }));
            }
            
            _counters[metric.GetType()].WithLabels(metric.MetricValues().ToArray()).Inc(increment);
        }

        public void Observe(object metric, double duration)
        {
            if (!_summaries.ContainsKey(metric.GetType())) {
                var (name, title) = metric.MetricDescription();
                _summaries.Add(metric.GetType(), Metrics.CreateSummary(name, title, new SummaryConfiguration() { LabelNames = metric.MetricFields().ToArray() }));
            }
            
            _summaries[metric.GetType()].WithLabels(metric.MetricValues().ToArray()).Observe(duration);
        }
    }
}