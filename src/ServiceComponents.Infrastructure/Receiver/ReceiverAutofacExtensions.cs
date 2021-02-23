using Autofac;

namespace ServiceComponents.Infrastructure.Receiver
{
    public static class ReceiverAutofacExtensions
    {
        /// <summary>
        /// Registers command, query and event receivers. Requires contexts.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static ContainerBuilder AddReceiver(this ContainerBuilder builder)
        {
            builder.RegisterType<CommandReceiver>().AsImplementedInterfaces().InstancePerDependency();
            builder.RegisterType<QueryReceiver>().AsImplementedInterfaces().InstancePerDependency();
            //builder.RegisterType<EventReceiver>().AsImplementedInterfaces().InstancePerDependency();

            return builder;
        }

        /// <summary>
        /// Adds behavior to enrich serilog with correlation, causation and user ids.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static ContainerBuilder AddReceiverLogEnricher(this ContainerBuilder builder)
        {
            builder.RegisterDecorator<CommandReceiverLogBehavior, IReceiveCommand>();
            builder.RegisterDecorator<QueryReceiverLogBehavior, IReceiveQuery>();
            builder.RegisterDecorator<EventReceiverLogBehavior, IReceiveEvent>();

            return builder;
        }


    }
}
