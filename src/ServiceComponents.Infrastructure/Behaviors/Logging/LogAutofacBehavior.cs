using Autofac;

namespace ServiceComponents.Infrastructure.Behaviors.Logging
{
    public static class LogAutofacBehavior
    {
        public static ContainerBuilder AddLogBehavior(this ContainerBuilder builder)
        {
            builder.RegisterType<LogBehavior>().AsImplementedInterfaces().InstancePerLifetimeScope();
            return builder;
        }
    }
}