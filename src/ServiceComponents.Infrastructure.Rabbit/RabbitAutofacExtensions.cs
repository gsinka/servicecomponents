using System;
using Autofac;
using RabbitMQ.Client;
using Serilog;
using ServiceComponents.Application.Senders;
using ServiceComponents.Infrastructure.Rabbit.Senders;

namespace ServiceComponents.Infrastructure.Rabbit
{
    public static class RabbitAutofacExtensions
    {
        public static ContainerBuilder AddRabbitConnection(this ContainerBuilder builder, Uri endpointUri,
            string clientName)
        {
            builder.Register(context => new ConnectionFactory() {

                Uri = endpointUri,
                AutomaticRecoveryEnabled = true,
                ClientProvidedName = clientName,

            }.CreateConnection()).As<IConnection>().SingleInstance();

            return builder;
        }

        public static ContainerBuilder AddRabbitChannel(this ContainerBuilder builder, object key = default)
        {
            var channelRegistration = builder.Register(context => context.Resolve<IConnection>().CreateModel())
                .SingleInstance();

            if (key == default) {
                channelRegistration.As<IModel>();
            }
            else {
                channelRegistration.Keyed<IModel>(key);
            }

            return builder;
        }

        public static ContainerBuilder AddRabbitEventPublisher(this ContainerBuilder builder, string exchange, string routingKey, bool mandatory = false, IBasicProperties basicProperties = null, object channelKey = default, string key = default)
        {
            var proxyRegistration = builder.Register(context => new RabbitEventPublisherProxy(
                channelKey == default ? context.Resolve<IModel>() : context.ResolveKeyed<IModel>(channelKey),
                key == default ? context.Resolve<IPublishRabbitEvent>() : context.ResolveKeyed<IPublishRabbitEvent>(key)
            )).InstancePerDependency();

            var senderRegistration = builder.Register(context => new RabbitEventPublisher(
                    context.Resolve<ILogger>(),
                    channelKey == default ? context.Resolve<IModel>() : context.ResolveKeyed<IModel>(channelKey),
                    exchange, routingKey))
                .InstancePerLifetimeScope();

            if (key == default) {
                proxyRegistration.As<IPublishEvent>();
                senderRegistration.As<IPublishRabbitEvent>();
            }
            else {
                proxyRegistration.Keyed<IPublishEvent>(key);
                senderRegistration.Keyed<IPublishRabbitEvent>(key);
            }

            return builder;
        }

        public static ContainerBuilder AddRabbitConsumer(this ContainerBuilder builder, string queue, string channelKey = default, string consumerKey = default)
        {
            var registration = builder.Register(context => new RabbitConsumer(
                    context.Resolve<ILogger>(),
                    context.Resolve<ILifetimeScope>(),
                    channelKey == default ? context.Resolve<IModel>() : context.ResolveKeyed<IModel>(channelKey),
                    queue))
                .SingleInstance();

            if (consumerKey == default) {
                registration.AsSelf();
            }
            else {
                registration.Keyed<RabbitConsumer>(consumerKey);
            }

            return builder;
        }

        public static ContainerBuilder AddRabbitReceivers(this ContainerBuilder builder)
        {
            builder.RegisterType<RabbitEventReceiver>().AsImplementedInterfaces().InstancePerDependency();
            builder.RegisterType<RabbitCommandReceiver>().AsImplementedInterfaces().InstancePerDependency();
            builder.RegisterType<RabbitQueryReceiver>().AsImplementedInterfaces().InstancePerDependency();

            return builder;
        }

        public static ContainerBuilder AddRabbitCommandSender(this ContainerBuilder builder, string exchange, string routingKey = default, object channelKey = default, string key = default)
        {
            var proxyRegistration = builder.Register(context => new RabbitCommandSenderProxy(
                channelKey == default ? context.Resolve<IModel>() : context.ResolveKeyed<IModel>(channelKey),
                key == default ? context.Resolve<ISendRabbitCommand>() : context.ResolveKeyed<ISendRabbitCommand>(key)
            )).InstancePerDependency();

            var senderRegistration = builder.Register(context => new RabbitCommandSender(
                    context.Resolve<ILogger>(),
                    channelKey == default ? context.Resolve<IModel>() : context.ResolveKeyed<IModel>(channelKey),
                    exchange, routingKey))
                .InstancePerLifetimeScope();

            if (key == default) {
                proxyRegistration.As<ISendCommand>();
                senderRegistration.As<ISendRabbitCommand>();
            }
            else {
                proxyRegistration.Keyed<ISendCommand>(key);
                senderRegistration.Keyed<ISendRabbitCommand>(key);
            }

            return builder;
        }

        public static ContainerBuilder AddRabbitQuerySender(this ContainerBuilder builder, string exchange, string routingKey = default, object channelKey = default, string key = default)
        {
            var proxyRegistration = builder.Register(context => new RabbitQuerySenderProxy(
                channelKey == default ? context.Resolve<IModel>() : context.ResolveKeyed<IModel>(channelKey),
                key == default ? context.Resolve<ISendRabbitQuery>() : context.ResolveKeyed<ISendRabbitQuery>(key)
            )).InstancePerDependency();

            var senderRegistration = builder.Register(context => new RabbitQuerySender(
                    context.Resolve<ILogger>(),
                    channelKey == default ? context.Resolve<IModel>() : context.ResolveKeyed<IModel>(channelKey),
                    exchange, routingKey))
                .InstancePerLifetimeScope();

            if (key == default) {
                proxyRegistration.As<ISendQuery>();
                senderRegistration.As<ISendRabbitQuery>();
            }
            else {
                proxyRegistration.Keyed<ISendQuery>(key);
                senderRegistration.Keyed<ISendRabbitQuery>(key);
            }

            return builder;
        }


        public static ContainerBuilder AddRabbitSenderCorrelationBehavior(this ContainerBuilder builder)
        {
            builder.RegisterDecorator<RabbitCommandSenderCorrelationBehavior, ISendRabbitCommand>();
            builder.RegisterDecorator<RabbitQuerySenderCorrelationBehavior, ISendRabbitQuery>();
            builder.RegisterDecorator<RabbitEventSenderCorrelationBehavior, IPublishRabbitEvent>();

            return builder;
        }

        public static ContainerBuilder AddRabbitReceiverCorrelationBehavior(this ContainerBuilder builder)
        {
            builder.RegisterDecorator<RabbitEventReceiverCorrelationBehavior, IReceiveRabbitEvent>();
            builder.RegisterDecorator<RabbitCommandReceiverCorrelationBehavior, IReceiveRabbitCommand>();
            builder.RegisterDecorator<RabbitQueryReceiverCorrelationBehavior, IReceiveRabbitQuery>();

            return builder;
        }
    }
}
