namespace ServiceComponents.Application.Monitoring
{
    public interface IMetricsService
    {
        void Increment(object metric, double increment = 1);

        void Observe(object metric, double duration);
        void Observe(object metric, double val, long count);
    }
}
