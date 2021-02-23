using Autofac;

namespace ServiceComponents.Infrastructure.Receivers
{
    public static class ReceiverAutofacExtensions
    {
        public static ContainerBuilder AddReceivers(this ContainerBuilder builder)
        {
            builder.RegisterType<CommandReceiver>().AsImplementedInterfaces().InstancePerDependency();
            builder.RegisterType<QueryReceiver>().AsImplementedInterfaces().InstancePerDependency();
            builder.RegisterType<EventReceiver>().AsImplementedInterfaces().InstancePerDependency();

            return builder;
        }
    }
}