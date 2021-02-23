using Autofac;
using Serilog;
using Serilog.Events;

namespace ServiceComponents.Infrastructure.Behaviors.Logging
{
    public static class LogAutofacBehavior
    {
        public static ContainerBuilder AddLogBehavior(this ContainerBuilder builder, LogEventLevel level = LogEventLevel.Debug)
        {
            builder.Register(context => new LogBehavior(context.Resolve<ILogger>(), level)).AsImplementedInterfaces().InstancePerLifetimeScope();
            return builder;
        }
    }
}