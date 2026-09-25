using System;
using System.Collections.Generic;
using Autofac;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using Serilog;
using ServiceComponents.Application.Senders;
using ServiceComponents.Infrastructure.Rabbit.Senders;

namespace ServiceComponents.Infrastructure.Rabbit
{
    public static class RabbitAutofacExtensions
    {
         /// <summary>
        /// Registers a RabbitMQ connection using IConfiguration with the "RabbitMQ" section.
        /// This method is provided for backward compatibility with existing applications.
        /// If IConfiguration is available in the container, it will be used for additional settings.
        /// </summary>
        /// <param name="builder">The Autofac container builder.</param>
        /// <param name="endpointUri">The RabbitMQ endpoint URI.</param>
        /// <param name="clientName">The client-provided name for identification.</param>
        /// <param name="key">Optional key for keyed registration.</param>
        /// <returns>The container builder for chaining.</returns>
        public static ContainerBuilder AddRabbitConnection(this ContainerBuilder builder, Uri endpointUri, string clientName, object key = default)
        {
            var registration = builder.Register(context => {
                var options = new RabbitConnectionOptions
                {
                    EndpointUri = endpointUri,
                    ClientName = clientName,
                    AutomaticRecoveryEnabled = true,
                    HandshakeContinuationTimeout = TimeSpan.FromSeconds(120)
                };

                // Try to load from configuration if available to get additional settings
                try
                {
                    var configuration = context.Resolve<IConfiguration>();
                    var configOptions = configuration.LoadFromConfiguration(nameof(RabbitConnectionOptions));
                    // Merge configuration settings (keep provided Uri and ClientName, but use config for other properties)
                    options.AutomaticRecoveryEnabled = configOptions.AutomaticRecoveryEnabled;
                    options.HandshakeContinuationTimeout = configOptions.HandshakeContinuationTimeout;
                    options.RequestedHeartbeat = configOptions.RequestedHeartbeat;
                    options.RequestedChannelMax = configOptions.RequestedChannelMax;
                    options.NetworkRecoveryInterval = configOptions.NetworkRecoveryInterval;
                    options.Ssl = configOptions.Ssl;
                    options.VirtualHost = configOptions.VirtualHost;
                }
                catch
                {
                    // Configuration not available or doesn't contain settings, just use defaults
                }

                return new ConnectionFactory
                {
                    Uri = options.EndpointUri,
                    AutomaticRecoveryEnabled = options.AutomaticRecoveryEnabled,
                    ClientProvidedName = options.ClientName,
                    HandshakeContinuationTimeout = options.HandshakeContinuationTimeout,
                    RequestedHeartbeat = options.RequestedHeartbeat,
                    RequestedChannelMax = options.RequestedChannelMax,
                    NetworkRecoveryInterval = options.NetworkRecoveryInterval,
                    Ssl = new SslOption { Enabled = options.Ssl },
                    VirtualHost = options.VirtualHost
                }.CreateConnectionAsync().GetAwaiter().GetResult();
            }).SingleInstance();

            if (key == default)
            {
                registration.As<IConnection>();
            }
            else
            {
                registration.Keyed<IConnection>(key);
            }

            return builder;
        }
        
        /// <summary>
        /// Registers a RabbitMQ connection using IConfiguration.
        /// Automatically loads configuration from appsettings using the "RabbitConnectionOptions" section.
        /// The IConfiguration is resolved from the container if not provided explicitly.
        /// For backward compatibility, can also accept Uri and clientName parameters directly.
        /// </summary>
        /// <param name="builder">The Autofac container builder.</param>
        /// <param name="configuration">Optional: explicit IConfiguration instance. If null, will be resolved from container.</param>
        /// <param name="key">Optional key for keyed registration.</param>
        /// <returns>The container builder for chaining.</returns>
        public static ContainerBuilder AddRabbitConnection(this ContainerBuilder builder, IConfiguration configuration = null, object key = default)
        {
            var registration = builder.Register(context => {
                var config = configuration ?? context.Resolve<IConfiguration>();
                var options = config.LoadFromConfiguration(nameof(RabbitConnectionOptions));
                return new ConnectionFactory
                {
                    Uri = options.EndpointUri,
                    AutomaticRecoveryEnabled = options.AutomaticRecoveryEnabled,
                    ClientProvidedName = options.ClientName,
                    HandshakeContinuationTimeout = options.HandshakeContinuationTimeout,
                    RequestedHeartbeat = options.RequestedHeartbeat,
                    RequestedChannelMax = options.RequestedChannelMax,
                    NetworkRecoveryInterval = options.NetworkRecoveryInterval,
                    Ssl = new SslOption { Enabled = options.Ssl },
                    VirtualHost = options.VirtualHost
                }.CreateConnectionAsync().GetAwaiter().GetResult();
            }).SingleInstance();

            if (key == default)
            {
                registration.As<IConnection>();
            }
            else
            {
                registration.Keyed<IConnection>(key);
            }

            return builder;
        }

        /// <summary>
        /// Registers a RabbitMQ connection using options class.
        /// This method allows configuration via appsettings or IOptions pattern.
        /// </summary>
        public static ContainerBuilder AddRabbitConnection(this ContainerBuilder builder, RabbitConnectionOptions options, object key = default)
        {
            if (options == null) {
                throw new ArgumentNullException(nameof(options));
            }

            options.Validate();

            var registration = builder.Register(context => new ConnectionFactory
            {
                Uri = options.EndpointUri,
                AutomaticRecoveryEnabled = options.AutomaticRecoveryEnabled,
                ClientProvidedName = options.ClientName,
                HandshakeContinuationTimeout = options.HandshakeContinuationTimeout,
                RequestedHeartbeat = options.RequestedHeartbeat,
                RequestedChannelMax = options.RequestedChannelMax,
                NetworkRecoveryInterval = options.NetworkRecoveryInterval,
                Ssl = new SslOption { Enabled = options.Ssl },
                VirtualHost = options.VirtualHost
            }.CreateConnectionAsync().GetAwaiter().GetResult()).SingleInstance();

            if (key == default)
            {
                registration.As<IConnection>();
            }
            else
            {
                registration.Keyed<IConnection>(key);
            }

            return builder;
        }

        /// <summary>
        /// Registers a RabbitMQ connection using IConfiguration with the "RabbitMQ" section.
        /// This method is a convenience for existing applications that use the "RabbitMQ" configuration section.
        /// The IConfiguration is resolved from the container if not provided explicitly.
        /// </summary>
        /// <param name="builder">The Autofac container builder.</param>
        /// <param name="configuration">Optional: explicit IConfiguration instance. If null, will be resolved from container.</param>
        /// <param name="key">Optional key for keyed registration.</param>
        /// <returns>The container builder for chaining.</returns>
        public static ContainerBuilder AddRabbitConnectionFromRabbitMQSection(this ContainerBuilder builder, IConfiguration configuration = null, object key = default)
        {
            var registration = builder.Register(context => {
                var config = configuration ?? context.Resolve<IConfiguration>();
                var options = config.LoadFromRabbitMQSection();
                return new ConnectionFactory
                {
                    Uri = options.EndpointUri,
                    AutomaticRecoveryEnabled = options.AutomaticRecoveryEnabled,
                    ClientProvidedName = options.ClientName,
                    HandshakeContinuationTimeout = options.HandshakeContinuationTimeout,
                    RequestedHeartbeat = options.RequestedHeartbeat,
                    RequestedChannelMax = options.RequestedChannelMax,
                    NetworkRecoveryInterval = options.NetworkRecoveryInterval,
                    Ssl = new SslOption { Enabled = options.Ssl },
                    VirtualHost = options.VirtualHost
                }.CreateConnectionAsync().GetAwaiter().GetResult();
            }).SingleInstance();

            if (key == default)
            {
                registration.As<IConnection>();
            }
            else
            {
                registration.Keyed<IConnection>(key);
            }

            return builder;
        }


        public static ContainerBuilder AddRabbitChannel(this ContainerBuilder builder, object connectionKey = default, object key = default)
        {
            var channelRegistration = builder.Register(context => connectionKey == default ? context.Resolve<IConnection>().CreateChannelAsync().GetAwaiter().GetResult() : context.ResolveKeyed<IConnection>(connectionKey).CreateChannelAsync().GetAwaiter().GetResult())
                .SingleInstance();

            if (key == default) {
                channelRegistration.As<IChannel>();
            }
            else {
                channelRegistration.Keyed<IChannel>(key);
            }

            return builder;
        }

        public static ContainerBuilder AddRabbitEventPublisher(this ContainerBuilder builder, string exchange, string routingKey, bool mandatory = false, BasicProperties basicProperties = null, object channelKey = default, string key = default)
        {
            var proxyRegistration = builder.Register(context => new RabbitEventPublisherProxy(
                channelKey == default ? context.Resolve<IChannel>() : context.ResolveKeyed<IChannel>(channelKey),
                key == default ? context.Resolve<IPublishRabbitEvent>() : context.ResolveKeyed<IPublishRabbitEvent>(key)
            )).InstancePerDependency();

            var senderRegistration = builder.Register(context => new RabbitEventPublisher(
                    context.Resolve<ILogger>(),
                    channelKey == default ? context.Resolve<IChannel>() : context.ResolveKeyed<IChannel>(channelKey),
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
                    channelKey == default ? context.Resolve<IChannel>() : context.ResolveKeyed<IChannel>(channelKey),
                    queue, 
                    consumerTag))
                .AsImplementedInterfaces()
                .SingleInstance();

            if (consumerKey == default) {
                registration.AsSelf();
                registration.Keyed<RabbitConsumer>("__consumer__");
            }
            else {
                registration.Keyed<RabbitConsumer>(consumerKey);
                registration.Keyed<RabbitConsumer>("__consumer__");
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
                channelKey == default ? context.Resolve<IChannel>() : context.ResolveKeyed<IChannel>(channelKey),
                key == default ? context.Resolve<ISendRabbitCommand>() : context.ResolveKeyed<ISendRabbitCommand>(key)
            )).InstancePerDependency();

            var senderRegistration = builder.Register(context => new RabbitCommandSender(
                    context.Resolve<ILogger>(),
                    channelKey == default ? context.Resolve<IChannel>() : context.ResolveKeyed<IChannel>(channelKey),
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
                channelKey == default ? context.Resolve<IChannel>() : context.ResolveKeyed<IChannel>(channelKey),
                key == default ? context.Resolve<ISendRabbitQuery>() : context.ResolveKeyed<ISendRabbitQuery>(key)
            )).InstancePerDependency();

            var senderRegistration = builder.Register(context => new RabbitQuerySender(
                    context.Resolve<ILogger>(),
                    channelKey == default ? context.Resolve<IChannel>() : context.ResolveKeyed<IChannel>(channelKey),
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
            builder.AddRabbitChannel(connectionKey: connectionKey, key: "consumer-retry");
            
            foreach (var ttl in ttls) {
                builder.AddRabbitConsumer($"{queue}-retry-{ttl}", $"{clientName}-consumer-retry-{ttl}", "consumer-retry", $"consumer-retry-{ttl}");
            }

            return builder;
        }

        public static IChannel AddRabbitRetry(this IChannel channel, ILifetimeScope scope, string queue, int [] ttls)
        {
            channel.QueueDeclareAsync(queue, false, false, true, new Dictionary<string, object> { { "x-dead-letter-exchange", $"{queue}-dlx-wait-{ttls[0]}" } }).GetAwaiter().GetResult();

            for (var i = 0; i < ttls.Length; i++) {

                channel.ExchangeDeclareAsync($"{queue}-dlx-wait-{ttls[i]}", "direct", false, true).GetAwaiter().GetResult();
                channel.ExchangeDeclareAsync($"{queue}-dlx-retry-{ttls[i]}", "direct", false, true).GetAwaiter().GetResult();

                channel.QueueDeclareAsync($"{queue}-wait-{ttls[i]}", false, false, true, new Dictionary<string, object> { { "x-message-ttl", ttls[i] }, { "x-dead-letter-exchange", $"{queue}-dlx-retry-{ttls[i]}" }}).GetAwaiter().GetResult();
                channel.QueueDeclareAsync($"{queue}-retry-{ttls[i]}", false, false, true, new Dictionary<string, object> { { "x-dead-letter-exchange", i == ttls.Length - 1 ? $"{queue}-dlx" : $"{queue}-dlx-wait-{ttls[i + 1]}" } }).GetAwaiter().GetResult();
                channel.QueueBindAsync($"{queue}-wait-{ttls[i]}", $"{queue}-dlx-wait-{ttls[i]}", string.Empty).GetAwaiter().GetResult();
                channel.QueueBindAsync($"{queue}-retry-{ttls[i]}", $"{queue}-dlx-retry-{ttls[i]}", string.Empty).GetAwaiter().GetResult();

                //scope.ResolveKeyed<RabbitConsumer>($"consumer-retry-{ttls[i]}").StartAsync(CancellationToken.None).Wait();
            }

            channel.ExchangeDeclareAsync($"{queue}-dlx", "direct", false, true).GetAwaiter().GetResult();
            channel.QueueDeclareAsync($"{queue}-dlx", false, false, true).GetAwaiter().GetResult();
            channel.QueueBindAsync($"{queue}-dlx", $"{queue}-dlx", string.Empty).GetAwaiter().GetResult();
            
            return channel;
        }

    }
}
