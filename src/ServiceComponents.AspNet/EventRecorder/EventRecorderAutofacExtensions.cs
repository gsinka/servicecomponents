using Autofac;

namespace ServiceComponents.AspNet.EventRecorder
{
    public static class EventRecorderAutofacExtensions
    {
        public static ContainerBuilder UseEventRecorder(this ContainerBuilder builder)
        {
            builder.RegisterType<EventRecorderService>().SingleInstance();
            builder.RegisterType<EventRecorderFeeder>().AsImplementedInterfaces().InstancePerDependency();

            return builder;
        }
    }
}
