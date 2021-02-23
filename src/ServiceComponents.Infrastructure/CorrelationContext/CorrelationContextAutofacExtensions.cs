using Autofac;

namespace ServiceComponents.Infrastructure.CorrelationContext
{
    public static class CorrelationContextAutofacExtensions
    {
        public static ContainerBuilder AddCorrelationInfo(this ContainerBuilder builder)
        {
            builder.RegisterType<CorrelationContext.Correlation>().AsImplementedInterfaces().AsSelf().InstancePerLifetimeScope();
            return builder;
        }
    }
}
