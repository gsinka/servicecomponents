namespace ServiceComponents.Application.Monitoring
{
    public static class MetricServiceExtensions
    {
        public static DurationMetric<T> Observe<T>(this IMetricsService service, T metric)
        {
            return new DurationMetric<T>(service, metric);
        }
    }
}