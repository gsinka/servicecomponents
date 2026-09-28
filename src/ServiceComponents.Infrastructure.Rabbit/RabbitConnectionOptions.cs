using System;

namespace ServiceComponents.Infrastructure.Rabbit;

/// <summary>
/// Options for configuring RabbitMQ connection settings.
/// These options can be populated from appsettings via dependency injection or manually.
/// All properties map to RabbitMQ.Client.ConnectionFactory settings.
/// </summary>
public class RabbitConnectionOptions
{
    public const string ConfigurationSectionName = "ServiceComponents:RabbitConnectionOptions";

    /// <summary>
    /// Gets or sets the RabbitMQ endpoint URI.
    /// Example: "amqp://guest:guest@localhost:5672/"
    /// </summary>
    public Uri EndpointUri { get; set; }

    /// <summary>
    /// Gets or sets the client-provided name for identification.
    /// This name is used in RabbitMQ logs for identifying the connection source.
    /// </summary>
    public string ClientName { get; set; }

    /// <summary>
    /// Gets or sets whether automatic recovery is enabled.
    /// Default: true
    /// </summary>
    public bool AutomaticRecoveryEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the handshake continuation timeout duration.
    /// Default: 120 seconds
    /// </summary>
    public TimeSpan HandshakeContinuationTimeout { get; set; } = TimeSpan.FromSeconds(120);

    /// <summary>
    /// Gets or sets the requested heartbeat timeout.
    /// Default: 60 seconds
    /// </summary>
    public TimeSpan RequestedHeartbeat { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Gets or sets the requested channel maximum.
    /// Default: 2048
    /// </summary>
    public ushort RequestedChannelMax { get; set; } = 2048;


    /// <summary>
    /// Gets or sets the network recovery interval.
    /// Default: 5 seconds
    /// </summary>
    public TimeSpan NetworkRecoveryInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets whether SSL/TLS is enabled.
    /// Default: false
    /// </summary>
    public bool Ssl { get; set; } = false;

    /// <summary>
    /// Gets or sets the virtual host to connect to.
    /// Default: "/" (can be overridden if not in URI)
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Validates that required properties are set.
    /// </summary>
    /// <returns>True if valid, throws ArgumentException if not.</returns>
    public void Validate()
    {
        if (EndpointUri == null) {
            throw new ArgumentException("EndpointUri is required for RabbitMQ connection configuration.");
        }

        if (string.IsNullOrWhiteSpace(ClientName)) {
            throw new ArgumentException("ClientName is required for RabbitMQ connection configuration.");
        }
    }
}