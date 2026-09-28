using System;
using System.Collections.Generic;
using System.Threading;
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
    /// </summary>
    public static ContainerBuilder AddRabbitConnection(this ContainerBuilder builder, Uri endpointUri,
        string clientName, object key = default)
    {
        var registration = builder
            .Register(context => {
                var options = new RabbitConnectionOptions();
                context.ResolveOptional<IConfiguration>()?
                    .GetSection(RabbitConnectionOptions.ConfigurationSectionName).Bind(options);

                options.EndpointUri = endpointUri;
                options.ClientName = clientName;
                options.Validate();

                return CreateConnectionFactory(options).CreateConnectionAsync().GetAwaiter().GetResult();
            }).SingleInstance();

        if (key == default) {
            registration.As<IConnection>();
        }
        else {
            registration.Keyed<IConnection>(key);
        }

        return builder;
    }

    /// <summary>
    /// Registers a singleton connection for asynchronous initialization after the container is built.
    /// Configuration is applied before opening the connection; the supplied URI and client name take precedence.
    /// This method performs no network I/O and is safe to call from a synchronous ConfigureContainer callback.
    /// </summary>
    public static ContainerBuilder RegisterRabbitConnection(this ContainerBuilder builder, Uri endpointUri,
        string clientName, object key = default)
    {
        ArgumentNullException.ThrowIfNull(endpointUri);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientName);

        return builder.RegisterRabbitConnection(context => {
            var options = new RabbitConnectionOptions();
            context.ResolveOptional<IConfiguration>()?
                .GetSection(RabbitConnectionOptions.ConfigurationSectionName).Bind(options);
            options.EndpointUri = endpointUri;
            options.ClientName = clientName;
            options.Validate();

            return CreateConnectionFactory(options);
        }, key);
    }

    /// <summary>
    /// Registers a singleton connection using a custom connection factory, without opening it during registration.
    /// The factory is obtained from the built container before asynchronous initialization starts.
    /// </summary>
    public static ContainerBuilder RegisterRabbitConnection(this ContainerBuilder builder,
        Func<IComponentContext, IConnectionFactory> connectionFactory, object key = default)
    {
        ArgumentNullException.ThrowIfNull(connectionFactory);

        return RegisterRabbitResource(builder, context => {
            var factory = connectionFactory(context);
            ArgumentNullException.ThrowIfNull(factory);
            return new RabbitAsyncResource<IConnection>(token => factory.CreateConnectionAsync(token), 0);
        }, key);
    }

    /// <summary>
    /// Registers a RabbitMQ channel.
    /// </summary>
    public static ContainerBuilder AddRabbitChannel(this ContainerBuilder builder, object connectionKey = default,
        object key = default)
    {
        var channelRegistration = builder
            .Register(context => connectionKey == default
                ? context.Resolve<IConnection>().CreateChannelAsync().GetAwaiter().GetResult()
                : context.ResolveKeyed<IConnection>(connectionKey).CreateChannelAsync().GetAwaiter().GetResult())
            .SingleInstance();

        if (key == default) {
            channelRegistration.As<IChannel>();
        }
        else {
            channelRegistration.Keyed<IChannel>(key);
        }

        return builder;
    }

    /// <summary>
    /// Registers a singleton channel for asynchronous initialization, using the default or keyed connection.
    /// Await <see cref="InitializeRabbitAsync"/> after Build and before resolving the channel or its consumers.
    /// </summary>
    public static ContainerBuilder RegisterRabbitChannel(this ContainerBuilder builder,
        object connectionKey = default, object key = default)
    {
        return RegisterRabbitResource(builder, context => {
            // Autofac's registration context must not be captured across an await. Retain the owning scope instead.
            var scope = context.Resolve<ILifetimeScope>();
            return new RabbitAsyncResource<IChannel>(token => {
                var connection = connectionKey == default
                    ? scope.Resolve<IConnection>()
                    : scope.ResolveKeyed<IConnection>(connectionKey);
                return connection.CreateChannelAsync(cancellationToken: token);
            }, 1);
        }, key);
    }

    /// <summary>
    /// Asynchronously opens all registered connections, then their channels, without modifying the built container.
    /// Call on the root scope after Build and before resolving RabbitMQ-dependent services or starting the host.
    /// Concurrent calls share initialization; the first call's token controls it. Failures are propagated and
    /// partially created resources are disposed. Dispose the container asynchronously at shutdown.
    /// </summary>
    public static Task InitializeRabbitAsync(this ILifetimeScope scope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scope);
        cancellationToken.ThrowIfCancellationRequested();
        return scope.ResolveOptional<RabbitAsyncInitializer>()?.InitializeAsync(cancellationToken) ??
               Task.CompletedTask;
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
            HandshakeContinuationTimeout = TimeSpan.FromSeconds(options.HandshakeContinuationTimeoutInSeconds),
            RequestedHeartbeat = TimeSpan.FromSeconds(options.RequestedHeartbeatInSeconds),
            RequestedChannelMax = options.RequestedChannelMaxInSeconds,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(options.NetworkRecoveryIntervalInSeconds),
            Ssl = new SslOption { Enabled = options.Ssl },
            VirtualHost = options.VirtualHost
        };
    }


    private static ContainerBuilder RegisterRabbitResource<T>(ContainerBuilder builder,
        Func<IComponentContext, RabbitAsyncResource<T>> factory, object key)
        where T : class, IDisposable, IAsyncDisposable
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.RegisterType<RabbitAsyncInitializer>().SingleInstance().IfNotRegistered(typeof(RabbitAsyncInitializer));

        var resourceKey = new object();
        builder.Register(factory)
            .Keyed<RabbitAsyncResource<T>>(resourceKey)
            .As<IRabbitAsyncResource>()
            .SingleInstance()
            .ExternallyOwned();

        // The initializer owns disposal, including resources never resolved by an application service.
        var registration = builder.Register(context => context.ResolveKeyed<RabbitAsyncResource<T>>(resourceKey).Value)
            .SingleInstance()
            .ExternallyOwned();

        if (key == default) {
            registration.As<T>();
        }
        else {
            registration.Keyed<T>(key);
        }

        return builder;
    }
}