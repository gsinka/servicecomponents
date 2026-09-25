using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac;
using Autofac.Builder;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using Serilog;
using ServiceComponents.Application.Senders;
using ServiceComponents.Infrastructure.Rabbit.Senders;

namespace ServiceComponents.Infrastructure.Rabbit;

public static class RabbitAutofacExtensions
{
    /// <summary>
    /// Registers a RabbitMQ connection. This method is provided for backward compatibility with existing applications.
    /// If IConfiguration is available in the container, it will be used for additional settings.
    /// Consider using <see cref="AddRabbitConnectionAsync"/> instead for better async/await support.
    /// </summary>
    public static ContainerBuilder AddRabbitConnection(this ContainerBuilder builder, Uri endpointUri,
        string clientName, object key = default)
    {
        var registration = builder
            .Register(context => {
                var options = new RabbitConnectionOptions {
                    EndpointUri = endpointUri,
                    ClientName = clientName,
                    AutomaticRecoveryEnabled = true,
                    HandshakeContinuationTimeout = TimeSpan.FromSeconds(120)
                };

                try {
                    var configuration = context.Resolve<IConfiguration>();
                    var configOptions = configuration.LoadFromConfiguration();
                    MergeConfigurationSettings(options, configOptions);

                    options.Validate();
                }
                catch {
                    // Configuration not available or doesn't contain settings, just use defaults
                }

                return CreateConnectionFactory(options).CreateConnectionAsync().GetAwaiter().GetResult();
            }).SingleInstance();

        RegisterConnectionWithKey(registration, key);
        return builder;
    }

    /// <summary>
    /// Registers a RabbitMQ connection with async/await support. This method is provided for backward compatibility with existing applications.
    /// If IConfiguration is available in the container, it will be used for additional settings.
    /// </summary>
    public static async Task<ContainerBuilder> AddRabbitConnectionAsync(this ContainerBuilder builder, Uri endpointUri,
        string clientName, object key = default)
    {
        var registration = builder
            .Register(async context => {
                var options = new RabbitConnectionOptions {
                    EndpointUri = endpointUri,
                    ClientName = clientName,
                    AutomaticRecoveryEnabled = true,
                    HandshakeContinuationTimeout = TimeSpan.FromSeconds(120)
                };

                try {
                    var configuration = context.Resolve<IConfiguration>();
                    var configOptions = configuration.LoadFromConfiguration();
                    MergeConfigurationSettings(options, configOptions);

                    options.Validate();
                }
                catch {
                    // Configuration not available or doesn't contain settings, just use defaults
                }

                return await CreateConnectionFactory(options).CreateConnectionAsync();
            }).SingleInstance();

        RegisterConnectionWithKey(registration, key);
        return builder;
    }

    /// <summary>
    /// Registers a RabbitMQ channel.
    /// Consider using <see cref="AddRabbitChannelAsync"/> instead for better async/await support.
    /// </summary>
    public static ContainerBuilder AddRabbitChannel(this ContainerBuilder builder, object connectionKey = default,
        object key = default)
    {
        var channelRegistration = builder
            .Register(context => connectionKey == default
                ? context.Resolve<IConnection>().CreateChannelAsync().GetAwaiter().GetResult()
                : context.ResolveKeyed<IConnection>(connectionKey).CreateChannelAsync().GetAwaiter().GetResult())
            .SingleInstance();

        RegisterChannelWithKey(channelRegistration, key);
        return builder;
    }

    /// <summary>
    /// Registers a RabbitMQ channel with async/await support.
    /// </summary>
    public static async Task<ContainerBuilder> AddRabbitChannelAsync(this ContainerBuilder builder,
        object connectionKey = default,
        object key = default)
    {
        var channelRegistration = builder
            .Register(async context => connectionKey == default
                ? await context.Resolve<IConnection>().CreateChannelAsync()
                : await context.ResolveKeyed<IConnection>(connectionKey).CreateChannelAsync())
            .SingleInstance();

        RegisterChannelWithKey(channelRegistration, key);
        return builder;
    }

    /// <summary>
    /// Registers a RabbitMQ event publisher.
    /// </summary>
    public static ContainerBuilder AddRabbitEventPublisher(this ContainerBuilder builder, string exchange,
        string routingKey, bool mandatory = false, BasicProperties basicProperties = null, object channelKey = default,
        string key = default)
    {
        IRegistrationBuilder<RabbitEventPublisherProxy, SimpleActivatorData, SingleRegistrationStyle>
            proxyRegistration = builder.Register(context => new RabbitEventPublisherProxy(
                channelKey == default ? context.Resolve<IChannel>() : context.ResolveKeyed<IChannel>(channelKey),
                key == default ? context.Resolve<IPublishRabbitEvent>() : context.ResolveKeyed<IPublishRabbitEvent>(key)
            )).InstancePerDependency();

        IRegistrationBuilder<RabbitEventPublisher, SimpleActivatorData, SingleRegistrationStyle> senderRegistration =
            builder.Register(context => new RabbitEventPublisher(
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

    /// <summary>
    /// Registers a RabbitMQ consumer.
    /// </summary>
    public static ContainerBuilder AddRabbitConsumer(this ContainerBuilder builder, string queue,
        string consumerTag = default, string channelKey = default, string consumerKey = default)
    {
        IRegistrationBuilder<RabbitConsumer, SimpleActivatorData, SingleRegistrationStyle> registration = builder
            .Register(context => new RabbitConsumer(
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

    /// <summary>
    /// Registers a RabbitMQ command sender.
    /// </summary>
    public static ContainerBuilder AddRabbitCommandSender(this ContainerBuilder builder, string exchange,
        string routingKey = default, object channelKey = default, string key = default)
    {
        IRegistrationBuilder<RabbitCommandSenderProxy, SimpleActivatorData, SingleRegistrationStyle> proxyRegistration =
            builder.Register(context => new RabbitCommandSenderProxy(
                channelKey == default ? context.Resolve<IChannel>() : context.ResolveKeyed<IChannel>(channelKey),
                key == default ? context.Resolve<ISendRabbitCommand>() : context.ResolveKeyed<ISendRabbitCommand>(key)
            )).InstancePerDependency();

        IRegistrationBuilder<RabbitCommandSender, SimpleActivatorData, SingleRegistrationStyle> senderRegistration =
            builder.Register(context => new RabbitCommandSender(
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

    /// <summary>
    /// Registers a RabbitMQ query sender.
    /// </summary>
    public static ContainerBuilder AddRabbitQuerySender(this ContainerBuilder builder, string exchange,
        string routingKey = default, object channelKey = default, string key = default)
    {
        IRegistrationBuilder<RabbitQuerySenderProxy, SimpleActivatorData, SingleRegistrationStyle> proxyRegistration =
            builder.Register(context => new RabbitQuerySenderProxy(
                channelKey == default ? context.Resolve<IChannel>() : context.ResolveKeyed<IChannel>(channelKey),
                key == default ? context.Resolve<ISendRabbitQuery>() : context.ResolveKeyed<ISendRabbitQuery>(key)
            )).InstancePerDependency();

        IRegistrationBuilder<RabbitQuerySender, SimpleActivatorData, SingleRegistrationStyle> senderRegistration =
            builder.Register(context => new RabbitQuerySender(
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

    public static ContainerBuilder AddRabbitRetryConsumers(this ContainerBuilder builder, string connectionKey,
        string queue, IEnumerable<int> ttls, string clientName)
    {
        builder.AddRabbitChannel(connectionKey, "consumer-retry");

        foreach (var ttl in ttls) {
            builder.AddRabbitConsumer($"{queue}-retry-{ttl}", $"{clientName}-consumer-retry-{ttl}", "consumer-retry",
                $"consumer-retry-{ttl}");
        }

        return builder;
    }

    /// <summary>
    /// Adds retry configuration to a RabbitMQ channel.
    /// Consider using <see cref="AddRabbitRetryAsync"/> instead for better async/await support.
    /// </summary>
    public static IChannel AddRabbitRetry(this IChannel channel, ILifetimeScope scope, string queue, int[] ttls)
    {
        channel.QueueDeclareAsync(queue, false, false, true,
                new Dictionary<string, object> { { "x-dead-letter-exchange", $"{queue}-dlx-wait-{ttls[0]}" } })
            .GetAwaiter()
            .GetResult();

        for (var i = 0; i < ttls.Length; i++) {
            channel.ExchangeDeclareAsync($"{queue}-dlx-wait-{ttls[i]}", "direct", false, true).GetAwaiter().GetResult();
            channel.ExchangeDeclareAsync($"{queue}-dlx-retry-{ttls[i]}", "direct", false, true).GetAwaiter()
                .GetResult();

            channel.QueueDeclareAsync($"{queue}-wait-{ttls[i]}", false, false, true,
                    new Dictionary<string, object>
                        { { "x-message-ttl", ttls[i] }, { "x-dead-letter-exchange", $"{queue}-dlx-retry-{ttls[i]}" } })
                .GetAwaiter().GetResult();
            channel.QueueDeclareAsync($"{queue}-retry-{ttls[i]}", false, false, true,
                new Dictionary<string, object> {
                    {
                        "x-dead-letter-exchange",
                        i == ttls.Length - 1 ? $"{queue}-dlx" : $"{queue}-dlx-wait-{ttls[i + 1]}"
                    }
                }).GetAwaiter().GetResult();
            channel.QueueBindAsync($"{queue}-wait-{ttls[i]}", $"{queue}-dlx-wait-{ttls[i]}", string.Empty).GetAwaiter()
                .GetResult();
            channel.QueueBindAsync($"{queue}-retry-{ttls[i]}", $"{queue}-dlx-retry-{ttls[i]}", string.Empty)
                .GetAwaiter().GetResult();

            //scope.ResolveKeyed<RabbitConsumer>($"consumer-retry-{ttls[i]}").StartAsync(CancellationToken.None).Wait();
        }

        channel.ExchangeDeclareAsync($"{queue}-dlx", "direct", false, true).GetAwaiter().GetResult();
        channel.QueueDeclareAsync($"{queue}-dlx", false, false, true).GetAwaiter().GetResult();
        channel.QueueBindAsync($"{queue}-dlx", $"{queue}-dlx", string.Empty).GetAwaiter().GetResult();

        return channel;
    }

    /// <summary>
    /// Adds retry configuration to a RabbitMQ channel with async/await support.
    /// </summary>
    public static async Task<IChannel> AddRabbitRetryAsync(this IChannel channel, ILifetimeScope scope, string queue,
        int[] ttls)
    {
        await channel.QueueDeclareAsync(queue, false, false, true,
            new Dictionary<string, object> { { "x-dead-letter-exchange", $"{queue}-dlx-wait-{ttls[0]}" } });

        for (var i = 0; i < ttls.Length; i++) {
            await channel.ExchangeDeclareAsync($"{queue}-dlx-wait-{ttls[i]}", "direct", false, true);
            await channel.ExchangeDeclareAsync($"{queue}-dlx-retry-{ttls[i]}", "direct", false, true);

            await channel.QueueDeclareAsync($"{queue}-wait-{ttls[i]}", false, false, true,
                new Dictionary<string, object>
                    { { "x-message-ttl", ttls[i] }, { "x-dead-letter-exchange", $"{queue}-dlx-retry-{ttls[i]}" } });
            await channel.QueueDeclareAsync($"{queue}-retry-{ttls[i]}", false, false, true,
                new Dictionary<string, object> {
                    {
                        "x-dead-letter-exchange",
                        i == ttls.Length - 1 ? $"{queue}-dlx" : $"{queue}-dlx-wait-{ttls[i + 1]}"
                    }
                });
            await channel.QueueBindAsync($"{queue}-wait-{ttls[i]}", $"{queue}-dlx-wait-{ttls[i]}", string.Empty);
            await channel.QueueBindAsync($"{queue}-retry-{ttls[i]}", $"{queue}-dlx-retry-{ttls[i]}", string.Empty);

            //scope.ResolveKeyed<RabbitConsumer>($"consumer-retry-{ttls[i]}").StartAsync(CancellationToken.None).Wait();
        }

        await channel.ExchangeDeclareAsync($"{queue}-dlx", "direct", false, true);
        await channel.QueueDeclareAsync($"{queue}-dlx", false, false, true);
        await channel.QueueBindAsync($"{queue}-dlx", $"{queue}-dlx", string.Empty);

        return channel;
    }

    /// <summary>
    /// Creates a ConnectionFactory from RabbitConnectionOptions.
    /// </summary>
    private static ConnectionFactory CreateConnectionFactory(RabbitConnectionOptions options)
    {
        return new ConnectionFactory {
            Uri = options.EndpointUri,
            AutomaticRecoveryEnabled = options.AutomaticRecoveryEnabled,
            ClientProvidedName = options.ClientName,
            HandshakeContinuationTimeout = options.HandshakeContinuationTimeout,
            RequestedHeartbeat = options.RequestedHeartbeat,
            RequestedChannelMax = options.RequestedChannelMax,
            NetworkRecoveryInterval = options.NetworkRecoveryInterval,
            Ssl = new SslOption { Enabled = options.Ssl },
            VirtualHost = options.VirtualHost
        };
    }

    /// <summary>
    /// Merges configuration settings into options while preserving provided Uri and ClientName.
    /// </summary>
    private static void MergeConfigurationSettings(RabbitConnectionOptions options,
        RabbitConnectionOptions configOptions)
    {
        options.AutomaticRecoveryEnabled = configOptions.AutomaticRecoveryEnabled;
        options.HandshakeContinuationTimeout = configOptions.HandshakeContinuationTimeout;
        options.RequestedHeartbeat = configOptions.RequestedHeartbeat;
        options.RequestedChannelMax = configOptions.RequestedChannelMax;
        options.NetworkRecoveryInterval = configOptions.NetworkRecoveryInterval;
        options.Ssl = configOptions.Ssl;
        options.VirtualHost = configOptions.VirtualHost;
    }

    /// <summary>
    /// Registers a connection with the appropriate interfaces based on the key.
    /// </summary>
    private static void RegisterConnectionWithKey(dynamic registration, object key)
    {
        if (key == default) {
            registration.As<IConnection>();
        }
        else {
            registration.Keyed<IConnection>(key);
        }
    }

    /// <summary>
    /// Registers a channel with the appropriate interfaces based on the key.
    /// </summary>
    private static void RegisterChannelWithKey(dynamic registration, object key)
    {
        if (key == default) {
            registration.As<IChannel>();
        }
        else {
            registration.Keyed<IChannel>(key);
        }
    }
}