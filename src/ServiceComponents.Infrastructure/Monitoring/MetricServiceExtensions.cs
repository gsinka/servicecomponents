using ServiceComponents.Application.Monitoring;

namespace ServiceComponents.Infrastructure.Monitoring
{
    public static class MetricServiceExtensions
    {
        public static DurationMetric<T> Observe<T>(this IMetricsService service, T metric)
        {
            return new(service, metric);
        }
    }
}