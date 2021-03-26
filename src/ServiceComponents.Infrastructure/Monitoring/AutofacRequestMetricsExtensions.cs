using Autofac;
using ServiceComponents.Application.Monitoring;
using ServiceComponents.Infrastructure.Receivers;

namespace ServiceComponents.Infrastructure.Monitoring
{
    public static class AutofacRequestMetricsExtensions
    {
        public static ContainerBuilder AddPrometheusRequestMetricsBehavior(this ContainerBuilder builder)
        {
            builder.RegisterType<PrometheusMetricsService>().As<IMetricsService>().SingleInstance();
            builder.RegisterDecorator<PrometheusCommandMetricsBehavior, IReceiveCommand>();
            builder.RegisterDecorator<PrometheusQueryMetricsBehavior, IReceiveQuery>();
            builder.RegisterDecorator<PrometheusEventMetricsBehavior, IReceiveEvent>();

            return builder;
        }
    }
}
