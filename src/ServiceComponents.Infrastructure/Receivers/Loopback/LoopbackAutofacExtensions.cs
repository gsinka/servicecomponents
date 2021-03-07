using Autofac;
using ServiceComponents.Application.Senders;
using ServiceComponents.Infrastructure.Receivers;

namespace ServiceComponents.Infrastructure.Senders
{
    public static class LoopbackAutofacExtensions
    {
        public static ContainerBuilder AddLoopbackCommandSender(this ContainerBuilder builder, object key = default)
        {
            var proxyRegistration = builder.Register(context =>
                new LoopbackCommandSenderProxy(key == default
                    ? context.Resolve<ISendLoopbackCommand>()
                    : context.ResolveKeyed<ISendLoopbackCommand>(key))).InstancePerDependency();

            var senderRegistration = builder.RegisterType<LoopbackCommandSender>().InstancePerLifetimeScope();

            if (key == default) {
                proxyRegistration.As<ISendCommand>();
                senderRegistration.As<ISendLoopbackCommand>();
            }
            else {
                proxyRegistration.Keyed<ISendCommand>(key);
                senderRegistration.Keyed<ISendLoopbackCommand>(key);
            }

            return builder;
        }

        public static ContainerBuilder AddLoopbackQuerySender(this ContainerBuilder builder, object key = default)
        {
            var proxyRegistration = builder.Register(context =>
                new LoopbackQuerySenderProxy(key == default
                    ? context.Resolve<ISendLoopbackQuery>()
                    : context.ResolveKeyed<ISendLoopbackQuery>(key))).InstancePerDependency();

            var registration = builder.RegisterType<LoopbackQuerySender>().InstancePerLifetimeScope();

            if (key == default) {
                proxyRegistration.As<ISendQuery>();
                registration.As<ISendLoopbackQuery>();
            }
            else {
                proxyRegistration.Keyed<ISendQuery>(key);
                registration.Keyed<ISendLoopbackQuery>(key);
            }

            return builder;
        }

        public static ContainerBuilder AddLoopbackEventPublisher(this ContainerBuilder builder, object key = default)
        {
            var proxyRegistration = builder.Register(context =>
                new LoopbackEventPublisherProxy(key == default
                    ? context.Resolve<IPublishLoopbackEvent>()
                    : context.ResolveKeyed<IPublishLoopbackEvent>(key))).InstancePerDependency();

            var registration = builder.RegisterType<LoopbackEventPublisher>().InstancePerLifetimeScope();
            if (key == default) {
                proxyRegistration.As<IPublishEvent>();
                registration.As<IPublishLoopbackEvent>();
            }
            else {
                proxyRegistration.Keyed<IPublishEvent>(key);
                registration.Keyed<IPublishLoopbackEvent>(key);
            }

            return builder;
        }

        public static ContainerBuilder AddLoopbackReceiverCorrelationBehavior(this ContainerBuilder builder)
        {
            builder.RegisterDecorator<LoopbackCommandReceiverCorrelationBehavior, IReceiveLoopbackCommand>();
            builder.RegisterDecorator<LoopbackQueryReceiverCorrelationBehavior, IReceiveLoopbackQuery>();
            builder.RegisterDecorator<LoopbackEventReceiverCorrelationBehavior, IReceiveLoopbackEvent>();

            return builder;
        }

        public static ContainerBuilder AddLoopbackReceivers(this ContainerBuilder builder)
        {
            builder.RegisterType<LoopbackCommandReceiver>().As<IReceiveLoopbackCommand>().InstancePerLifetimeScope();
            builder.RegisterType<LoopbackQueryReceiver>().As<IReceiveLoopbackQuery>().InstancePerLifetimeScope();
            builder.RegisterType<LoopbackEventReceiver>().As<IReceiveLoopbackEvent>().InstancePerLifetimeScope();

            return builder;
        }
    }
}