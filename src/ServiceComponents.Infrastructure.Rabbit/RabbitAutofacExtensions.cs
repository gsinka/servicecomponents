using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Autofac;
using Autofac.Features.ResolveAnything;
using RabbitMQ.Client;
using Serilog;
using ServiceComponents.Application.Senders;
using ServiceComponents.Infrastructure.Rabbit.Senders;

namespace ServiceComponents.Infrastructure.Rabbit
{
    public static class RabbitAutofacExtensions
    {
        public static ContainerBuilder AddRabbitConnection(this ContainerBuilder builder, Uri endpointUri, string clientName, object key = default)
        {
            var registrtion = builder.Register(context => new ConnectionFactory() {

                Uri = endpointUri,
                AutomaticRecoveryEnabled = true,
                ClientProvidedName = clientName,

            }.CreateConnection()).SingleInstance();

            if (key == default) {
                registrtion.As<IConnection>();
            }
            else {
                registrtion.Keyed<IConnection>(key);
            }

            return builder;
        }

        public static ContainerBuilder AddRabbitChannel(this ContainerBuilder builder, object connectionKey = default, object key = default)
        {
            var channelRegistration = builder.Register(context => connectionKey == default ? context.Resolve<IConnection>().CreateModel() : context.ResolveKeyed<IConnection>(connectionKey).CreateModel())
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

        public static ContainerBuilder AddRabbitConsumer(this ContainerBuilder builder, string queue, string consumerTag = default, string channelKey = default, string consumerKey = default)
        {
            var registration = builder.Register(context => new RabbitConsumer(
                    context.Resolve<ILogger>(),
                    context.Resolve<ILifetimeScope>(),
                    channelKey == default ? context.Resolve<IModel>() : context.ResolveKeyed<IModel>(channelKey),
                    queue, 
                    consumerTag))
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

        public static ContainerBuilder AddRabbitRetryConsumers(this ContainerBuilder builder, string connectionKey, string queue, IEnumerable<int> ttls, string clientName)
        {
            builder.AddRabbitChannel(connectionKey: connectionKey, key: $"consumer-retry");
            
            foreach (var ttl in ttls) {
                builder.AddRabbitConsumer($"{queue}-retry-{ttl}", $"{clientName}-consumer-retry-{ttl}", $"consumer-retry", $"consumer-retry-{ttl}");
            }

            return builder;
        }

        public static IModel AddRabbitRetry(this IModel channel, ILifetimeScope scope, string queue, int [] ttls)
        {
            channel.QueueDeclare(queue, false, false, true, new Dictionary<string, object>() { { "x-dead-letter-exchange", $"{queue}-dlx-wait-{ttls[0]}" } });

            for (var i = 0; i < ttls.Length; i++) {

                channel.ExchangeDeclare($"{queue}-dlx-wait-{ttls[i]}", "direct", false, true, null);
                channel.ExchangeDeclare($"{queue}-dlx-retry-{ttls[i]}", "direct", false, true, null);

                channel.QueueDeclare($"{queue}-wait-{ttls[i]}", false, false, true, new Dictionary<string, object>() { { "x-message-ttl", ttls[i] }, { "x-dead-letter-exchange", $"{queue}-dlx-retry-{ttls[i]}" }});
                channel.QueueDeclare($"{queue}-retry-{ttls[i]}", false, false, true, new Dictionary<string, object>() { { "x-dead-letter-exchange", i == ttls.Length - 1 ? $"{queue}-dlx" : $"{queue}-dlx-wait-{ttls[i + 1]}" } });
                channel.QueueBind($"{queue}-wait-{ttls[i]}", $"{queue}-dlx-wait-{ttls[i]}", string.Empty);
                channel.QueueBind($"{queue}-retry-{ttls[i]}", $"{queue}-dlx-retry-{ttls[i]}", string.Empty);

                scope.ResolveKeyed<RabbitConsumer>($"consumer-retry-{ttls[i]}").StartAsync(CancellationToken.None).Wait();
            }

            channel.ExchangeDeclare($"{queue}-dlx", "direct", false, true, null);
            channel.QueueDeclare($"{queue}-dlx", false, false, true);
            channel.QueueBind($"{queue}-dlx", $"{queue}-dlx", string.Empty);
            
            return channel;
        }

    }
}
