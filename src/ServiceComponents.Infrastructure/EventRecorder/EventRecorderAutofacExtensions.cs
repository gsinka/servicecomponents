using Autofac;

namespace ServiceComponents.Infrastructure.EventRecorder
{
    public static class EventRecorderAutofacExtensions
    {
        public static ContainerBuilder UseEventRecorder(this ContainerBuilder builder)
        {
            builder.RegisterType<EventRecorderService>().AsSelf().AsImplementedInterfaces().SingleInstance();
            builder.RegisterType<EventRecorderFeeder>().AsImplementedInterfaces().InstancePerDependency();

            return builder;
        }
    }
}
