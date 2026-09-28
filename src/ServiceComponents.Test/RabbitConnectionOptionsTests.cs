using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using ServiceComponents.Infrastructure.Rabbit;
using Xunit;

namespace ServiceComponents.Test;

public class RabbitConnectionOptionsTests
{
    [Fact]
    public void Missing_configuration_keeps_default_timeout_values()
    {
        var factory = CreateConnectionFactory(new ConfigurationBuilder().Build());

        AssertTimeouts(factory, 120, 60, 5);
    }

    [Theory]
    [InlineData(120, 60, 5)]
    [InlineData(45, 30, 2)]
    [InlineData(12.5, 10, 0.25)]
    [InlineData(0, 0, 0)]
    public void Numeric_configuration_values_are_seconds(double handshake, double heartbeat, double recovery)
    {
        var section = RabbitConnectionOptions.ConfigurationSectionName;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> {
            [$"{section}:HandshakeContinuationTimeoutInSeconds"] = handshake.ToString(CultureInfo.InvariantCulture),
            [$"{section}:RequestedHeartbeatInSeconds"] = heartbeat.ToString(CultureInfo.InvariantCulture),
            [$"{section}:NetworkRecoveryIntervalInSeconds"] = recovery.ToString(CultureInfo.InvariantCulture)
        }).Build();

        var factory = CreateConnectionFactory(configuration);

        AssertTimeouts(factory, handshake, heartbeat, recovery);
    }

    [Fact]
    public void Example_configuration_uses_seconds()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "Rabbit.appsettings.example.json"))
            .Build();

        var factory = CreateConnectionFactory(configuration);

        AssertTimeouts(factory, 120, 60, 5);
    }

    [Theory]
    [InlineData(nameof(RabbitConnectionOptions.HandshakeContinuationTimeoutInSeconds))]
    [InlineData(nameof(RabbitConnectionOptions.RequestedHeartbeatInSeconds))]
    [InlineData(nameof(RabbitConnectionOptions.NetworkRecoveryIntervalInSeconds))]
    public void Invalid_timeout_configuration_identifies_the_property(string propertyName)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> {
            [$"{RabbitConnectionOptions.ConfigurationSectionName}:{propertyName}"] = "not-a-duration"
        }).Build();

        var error = Assert.Throws<InvalidOperationException>(() => configuration
            .GetSection(RabbitConnectionOptions.ConfigurationSectionName).Bind(new RabbitConnectionOptions()));

        Assert.Contains(propertyName, error.Message);
    }

    private static ConnectionFactory CreateConnectionFactory(IConfiguration configuration)
    {
        var options = new RabbitConnectionOptions {
            EndpointUri = new Uri("amqp://localhost"),
            ClientName = "configuration-test"
        };
        configuration.GetSection(RabbitConnectionOptions.ConfigurationSectionName).Bind(options);
        options.Validate();

        // Exercise the mapping shared by both registration methods without opening a connection.
        var createFactory = typeof(RabbitAutofacExtensions).GetMethod("CreateConnectionFactory",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(createFactory);
        return Assert.IsType<ConnectionFactory>(createFactory.Invoke(null, new object[] { options }));
    }

    private static void AssertTimeouts(ConnectionFactory factory, double handshake, double heartbeat, double recovery)
    {
        Assert.Equal(TimeSpan.FromSeconds(handshake), factory.HandshakeContinuationTimeout);
        Assert.Equal(TimeSpan.FromSeconds(heartbeat), factory.RequestedHeartbeat);
        Assert.Equal(TimeSpan.FromSeconds(recovery), factory.NetworkRecoveryInterval);
    }
}