using Autofac;
using ServiceComponents.AspNet.Monitoring;

namespace ReferenceApplication.AspNet.Wireup
{
    public class MonitoringModule : Module
    {
        override protected void Load(ContainerBuilder builder)
        {
            builder.RegisterType<MetricsService>().SingleInstance();
        }
    }
}
