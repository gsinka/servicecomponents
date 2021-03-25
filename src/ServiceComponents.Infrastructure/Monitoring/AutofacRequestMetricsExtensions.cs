using Autofac;
using ServiceComponents.Infrastructure.Receivers;

namespace ServiceComponents.Infrastructure.Monitoring
{
    public static class AutofacRequestMetricsExtensions
    {
        public static ContainerBuilder AddPrometheusRequestMetricsBehavior(this ContainerBuilder builder)
        {
            builder.RegisterType<PrometheusMetricsService>().As<IMetricsService>().SingleInstance();
            builder.RegisterDecorator<PrometheusCommandMetricsBehavior, IReceiveCommand>();

            return builder;
        }
    }
}
