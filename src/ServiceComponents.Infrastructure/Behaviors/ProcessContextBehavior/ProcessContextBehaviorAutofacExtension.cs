using Autofac;

namespace ServiceComponents.Infrastructure.Behaviors.ProcessContextBehavior
{
    public static class ProcessContextBehaviorAutofacExtension
    {
        public static ContainerBuilder AddProcessContextBehavior(this ContainerBuilder builder, string titleKey, string contextCodeKey)
        {
            builder.RegisterInstance(new ProcessContextBehaviorOptions(titleKey, contextCodeKey)).AsSelf();
            builder.RegisterType<ProcessContextBehavior>().AsImplementedInterfaces().InstancePerLifetimeScope();

            return builder;
        }
    }
}
