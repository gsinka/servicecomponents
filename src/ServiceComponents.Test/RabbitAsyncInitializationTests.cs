using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using RabbitMQ.Client;
using ServiceComponents.Infrastructure.Rabbit;
using Xunit;

namespace ServiceComponents.Test;

public class RabbitAsyncInitializationTests
{
    [Fact]
    public async Task Build_is_network_free_and_initialization_awaits_both_connection_and_channel()
    {
        var connectionReady = CompletionSource<IConnection>();
        var channelReady = CompletionSource<IChannel>();
        var channelRequested = CompletionSource<bool>();
        var connection = new Mock<IConnection>();
        var channel = new Mock<IChannel>();
        var factory = new Mock<IConnectionFactory>();
        factory.Setup(value => value.CreateConnectionAsync(It.IsAny<CancellationToken>()))
            .Returns(connectionReady.Task);
        connection.Setup(value =>
                value.CreateChannelAsync(It.IsAny<CreateChannelOptions>(), It.IsAny<CancellationToken>()))
            .Callback(() => channelRequested.TrySetResult(true))
            .Returns(channelReady.Task);

        var builder = WebApplication.CreateBuilder();
        builder.Host.UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureContainer<ContainerBuilder>((_, containerBuilder) => {
                // Channels may be registered before their connections; registration performs no I/O.
                containerBuilder.RegisterRabbitChannel();
                containerBuilder.RegisterRabbitConnection(_ => factory.Object);
            });

        await using var app = builder.Build();
        var scope = app.Services.GetRequiredService<ILifetimeScope>();
        Assert.True(scope.IsRegistered<IConnection>());
        Assert.True(scope.IsRegistered<IChannel>());
        Assert.False(scope.IsRegistered<Task<IConnection>>());
        Assert.False(scope.IsRegistered<Task<IChannel>>());
        factory.Verify(value => value.CreateConnectionAsync(It.IsAny<CancellationToken>()), Times.Never);

        var initialization = scope.InitializeRabbitAsync();
        try {
            Assert.False(initialization.IsCompleted);
            Assert.Same(initialization, scope.InitializeRabbitAsync());
            connection.Verify(
                value => value.CreateChannelAsync(It.IsAny<CreateChannelOptions>(), It.IsAny<CancellationToken>()),
                Times.Never);
            connectionReady.SetResult(connection.Object);
            await channelRequested.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.False(initialization.IsCompleted);
            Assert.Throws<DependencyResolutionException>(() => scope.Resolve<IChannel>());
            channelReady.SetResult(channel.Object);
            await initialization.WaitAsync(TimeSpan.FromSeconds(10));

            Assert.Same(connection.Object, scope.Resolve<IConnection>());
            Assert.Same(channel.Object, scope.Resolve<IChannel>());
            await scope.InitializeRabbitAsync();
            factory.Verify(value => value.CreateConnectionAsync(It.IsAny<CancellationToken>()), Times.Once);
            connection.Verify(
                value => value.CreateChannelAsync(It.IsAny<CreateChannelOptions>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }
        finally {
            connectionReady.TrySetResult(connection.Object);
            channelReady.TrySetResult(channel.Object);
        }
    }

    [Fact]
    public async Task Keyed_and_default_resources_are_singletons_and_use_the_correct_connections()
    {
        var defaultConnection = new Mock<IConnection>();
        var publisherConnection = new Mock<IConnection>();
        var defaultChannel = new Mock<IChannel>();
        var publisherChannel = new Mock<IChannel>();
        SetupChannel(defaultConnection, defaultChannel);
        SetupChannel(publisherConnection, publisherChannel);

        var builder = new ContainerBuilder();
        builder.RegisterRabbitChannel("publisher", "publisher-channel");
        builder.RegisterRabbitChannel();
        builder.RegisterRabbitConnection(_ => Factory(defaultConnection).Object);
        builder.RegisterRabbitConnection(_ => Factory(publisherConnection).Object, "publisher");
        await using var container = builder.Build();
        await container.InitializeRabbitAsync();

        Assert.Same(defaultConnection.Object, container.Resolve<IConnection>());
        Assert.Same(publisherConnection.Object, container.ResolveKeyed<IConnection>("publisher"));
        Assert.Same(defaultChannel.Object, container.Resolve<IChannel>());
        Assert.Same(publisherChannel.Object, container.ResolveKeyed<IChannel>("publisher-channel"));
        await using var childScope = container.BeginLifetimeScope();
        await childScope.InitializeRabbitAsync();
        Assert.Same(defaultChannel.Object, childScope.Resolve<IChannel>());
        Assert.Same(publisherChannel.Object, childScope.ResolveKeyed<IChannel>("publisher-channel"));
        defaultConnection.Verify(
            value => value.CreateChannelAsync(It.IsAny<CreateChannelOptions>(), It.IsAny<CancellationToken>()),
            Times.Once);
        publisherConnection.Verify(
            value => value.CreateChannelAsync(It.IsAny<CreateChannelOptions>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Resolving_before_initialization_reports_the_required_startup_step()
    {
        var connection = new Mock<IConnection>();
        var channel = new Mock<IChannel>();
        SetupChannel(connection, channel);
        var builder = new ContainerBuilder();
        builder.RegisterRabbitConnection(_ => Factory(connection).Object);
        builder.RegisterRabbitChannel();
        await using var container = builder.Build();

        var error = Assert.Throws<DependencyResolutionException>(() => container.Resolve<IChannel>());
        Assert.Contains("InitializeRabbitAsync", error.GetBaseException().Message);
        await container.InitializeRabbitAsync();
        Assert.Same(channel.Object, container.Resolve<IChannel>());
    }

    [Fact]
    public async Task Factories_resolve_configuration_from_the_built_container_before_opening_connections()
    {
        var calls = 0;
        var connection = new Mock<IConnection>();
        var factory = Factory(connection);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> {
            ["RabbitTest:ClientName"] = "configured-client"
        }).Build();
        var builder = new ContainerBuilder();
        builder.RegisterRabbitConnection(context => {
            Assert.Equal("configured-client", context.Resolve<IConfiguration>()["RabbitTest:ClientName"]);
            calls++;
            return factory.Object;
        });
        builder.RegisterInstance(configuration).As<IConfiguration>();
        await using var container = builder.Build();
        Assert.Equal(0, calls);

        await container.InitializeRabbitAsync();
        Assert.Equal(1, calls);
        factory.Verify(value => value.CreateConnectionAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Invalid_optional_configuration_is_reported_instead_of_silently_ignored()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> {
            ["ServiceComponents:RabbitConnectionOptions:RequestedHeartbeat"] = "invalid-time-span"
        }).Build();
        var builder = new ContainerBuilder();
        builder.RegisterRabbitConnection(new Uri("amqp://localhost"), "configuration-test");
        builder.RegisterInstance(configuration).As<IConfiguration>();
        await using var container = builder.Build();

        var error = await Assert.ThrowsAsync<DependencyResolutionException>(() => container.InitializeRabbitAsync());
        Assert.Contains("RequestedHeartbeat", error.ToString());
    }

    [Fact]
    public async Task Failed_channel_initialization_cleans_up_partial_results_and_preserves_the_failure()
    {
        var disposed = new List<string>();
        var connection = new Mock<IConnection>();
        var channel = new Mock<IChannel>();
        var failure = new InvalidOperationException("Channel initialization failed");
        connection.SetupSequence(value =>
                value.CreateChannelAsync(It.IsAny<CreateChannelOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(channel.Object)
            .ThrowsAsync(failure);
        connection.Setup(value => value.DisposeAsync()).Callback(() => disposed.Add("connection"))
            .Returns(new ValueTask());
        channel.Setup(value => value.DisposeAsync()).Callback(() => disposed.Add("channel")).Returns(new ValueTask());
        var factory = Factory(connection);
        var builder = new ContainerBuilder();
        builder.RegisterRabbitConnection(_ => factory.Object);
        builder.RegisterRabbitChannel();
        builder.RegisterRabbitChannel(key: "failing-channel");
        await using var container = builder.Build();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => container.InitializeRabbitAsync());
        Assert.Same(failure, error);
        Assert.Equal(new[] { "channel", "connection" }, disposed);
        Assert.Same(failure,
            await Assert.ThrowsAsync<InvalidOperationException>(() => container.InitializeRabbitAsync()));
        await container.DisposeAsync();
        factory.Verify(value => value.CreateConnectionAsync(It.IsAny<CancellationToken>()), Times.Once);
        connection.Verify(value => value.DisposeAsync(), Times.Once);
        channel.Verify(value => value.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task Connection_failure_is_propagated_without_creating_channels()
    {
        var failure = new InvalidOperationException("Connection initialization failed");
        var factory = new Mock<IConnectionFactory>();
        factory.Setup(value => value.CreateConnectionAsync(It.IsAny<CancellationToken>())).ThrowsAsync(failure);
        var builder = new ContainerBuilder();
        builder.RegisterRabbitConnection(_ => factory.Object);
        builder.RegisterRabbitChannel();
        await using var container = builder.Build();

        Assert.Same(failure,
            await Assert.ThrowsAsync<InvalidOperationException>(() => container.InitializeRabbitAsync()));
        Assert.Throws<DependencyResolutionException>(() => container.Resolve<IChannel>());
    }

    [Fact]
    public async Task Cancellation_is_forwarded_and_cleans_up_opened_connections()
    {
        using var cancellation = new CancellationTokenSource();
        var connection = new Mock<IConnection>();
        var channel = new Mock<IChannel>();
        var receivedToken = CancellationToken.None;
        connection.Setup(value =>
                value.CreateChannelAsync(It.IsAny<CreateChannelOptions>(), It.IsAny<CancellationToken>()))
            .Returns(async (CreateChannelOptions _, CancellationToken token) => {
                receivedToken = token;
                await Task.Delay(Timeout.Infinite, token);
                return channel.Object;
            });
        var factory = Factory(connection);
        var builder = new ContainerBuilder();
        builder.RegisterRabbitConnection(_ => factory.Object);
        builder.RegisterRabbitChannel();
        await using var container = builder.Build();

        var initialization = container.InitializeRabbitAsync(cancellation.Token);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            initialization.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.Equal(cancellation.Token, receivedToken);
        factory.Verify(value => value.CreateConnectionAsync(cancellation.Token), Times.Once);
        connection.Verify(value => value.DisposeAsync(), Times.Once);
        channel.Verify(value => value.DisposeAsync(), Times.Never);
    }

    [Fact]
    public async Task Async_disposal_closes_unresolved_channels_before_connections_exactly_once()
    {
        var disposed = new List<string>();
        var connection = new Mock<IConnection>();
        var channel = new Mock<IChannel>();
        SetupChannel(connection, channel);
        connection.Setup(value => value.DisposeAsync()).Callback(() => disposed.Add("connection"))
            .Returns(new ValueTask());
        channel.Setup(value => value.DisposeAsync()).Callback(() => disposed.Add("channel")).Returns(new ValueTask());
        var builder = new ContainerBuilder();
        builder.RegisterRabbitConnection(_ => Factory(connection).Object);
        builder.RegisterRabbitChannel();
        await using var container = builder.Build();
        await container.InitializeRabbitAsync();

        // Never resolve IChannel; the initializer must still dispose the opened channel.
        await container.DisposeAsync();
        await container.DisposeAsync();
        Assert.Equal(new[] { "channel", "connection" }, disposed);
        connection.Verify(value => value.Dispose(), Times.Never);
        channel.Verify(value => value.Dispose(), Times.Never);
    }

    [Fact]
    public async Task Async_disposal_waits_for_an_inflight_connection_and_then_closes_it()
    {
        var connectionReady = CompletionSource<IConnection>();
        var connection = new Mock<IConnection>();
        var factory = new Mock<IConnectionFactory>();
        factory.Setup(value => value.CreateConnectionAsync(It.IsAny<CancellationToken>()))
            .Returns(connectionReady.Task);
        var builder = new ContainerBuilder();
        builder.RegisterRabbitConnection(_ => factory.Object);
        await using var container = builder.Build();
        var initialization = container.InitializeRabbitAsync();
        var disposal = container.DisposeAsync().AsTask();
        try {
            Assert.False(disposal.IsCompleted);
        }
        finally {
            connectionReady.TrySetResult(connection.Object);
        }

        await initialization.WaitAsync(TimeSpan.FromSeconds(10));
        await disposal.WaitAsync(TimeSpan.FromSeconds(10));
        connection.Verify(value => value.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task Initialization_without_async_registrations_is_a_noop()
    {
        await using var container = new ContainerBuilder().Build();
        await container.InitializeRabbitAsync();
    }

    private static Mock<IConnectionFactory> Factory(Mock<IConnection> connection)
    {
        var factory = new Mock<IConnectionFactory>();
        factory.Setup(value => value.CreateConnectionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(connection.Object);
        return factory;
    }

    private static void SetupChannel(Mock<IConnection> connection, Mock<IChannel> channel)
    {
        connection.Setup(value =>
                value.CreateChannelAsync(It.IsAny<CreateChannelOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(channel.Object);
    }

    private static TaskCompletionSource<T> CompletionSource<T>()
    {
        return new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}