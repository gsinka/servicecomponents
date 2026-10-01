namespace ServiceComponents.Application.Monitoring
{
    public interface IMetricsService
    {
        void Increment(object metric, double increment = 1);
        void Decrement(object metric, double decrement = 1);
        void Set(object metric, double target);

        void Observe(object metric, double duration);
        void Observe(object metric, double val, long count);
    }
}
