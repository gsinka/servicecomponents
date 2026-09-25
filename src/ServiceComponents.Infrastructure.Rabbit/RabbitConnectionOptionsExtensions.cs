using System;
using Microsoft.Extensions.Configuration;

namespace ServiceComponents.Infrastructure.Rabbit
{
    /// <summary>
    /// Extension methods for RabbitConnectionOptions configuration.
    /// </summary>
    public static class RabbitConnectionOptionsExtensions
    {
        private const string DefaultConfigurationSection = "ServiceComponents:RabbitConnectionOptions";

        /// <summary>
        /// Creates RabbitConnectionOptions from IConfiguration.
        /// Attempts to load from the specified section (or default "ServiceComponents:RabbitConnectionOptions" section).
        /// Falls back to provided defaults or default values for any missing properties.
        /// </summary>
        /// <param name="configuration">The configuration instance.</param>
        /// <param name="sectionName">The configuration section name. Defaults to "ServiceComponents:RabbitConnectionOptions".</param>
        /// <param name="defaultOptions">Optional default options to use as fallback. If null, built-in defaults are used.</param>
        /// <returns>Configured RabbitConnectionOptions instance.</returns>
        public static RabbitConnectionOptions LoadFromConfiguration(
            this IConfiguration configuration,
            string sectionName = DefaultConfigurationSection,
            RabbitConnectionOptions defaultOptions = null)
        {
            if (configuration == null) {
                throw new ArgumentNullException(nameof(configuration));
            }

            var options = new RabbitConnectionOptions();

            // Try to get the configuration section
            var configSection = configuration.GetSection(sectionName);

            if (configSection.Exists())
            {
                // Bind partial configuration - only sets properties that exist in configuration
                configSection.Bind(options);
            }

            // Validate that required properties are set
            options.Validate();

            return options;
        }

        /// <summary>
        /// Creates RabbitConnectionOptions with an alternative configuration path.
        /// This method supports nested configuration paths like "RabbitMQ:Connection".
        /// </summary>
        /// <param name="configuration">The configuration instance.</param>
        /// <param name="endpointUriPath">The configuration key path for EndpointUri (e.g., "RabbitMQ:EndpointUri").</param>
        /// <param name="clientNamePath">The configuration key path for ClientName (e.g., "RabbitMQ:ClientName").</param>
        /// <param name="automaticRecoveryEnabledPath">Optional path for AutomaticRecoveryEnabled. Defaults to built-in default if not found.</param>
        /// <param name="handshakeContinuationTimeoutPath">Optional path for HandshakeContinuationTimeout (in milliseconds). Defaults to built-in default if not found.</param>
        /// <returns>Configured RabbitConnectionOptions instance.</returns>
        public static RabbitConnectionOptions LoadFromConfigurationPaths(
            this IConfiguration configuration,
            string endpointUriPath,
            string clientNamePath,
            string automaticRecoveryEnabledPath = null,
            string handshakeContinuationTimeoutPath = null)
        {
            if (configuration == null) {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (string.IsNullOrWhiteSpace(endpointUriPath)) {
                throw new ArgumentException("endpointUriPath is required.", nameof(endpointUriPath));
            }

            if (string.IsNullOrWhiteSpace(clientNamePath)) {
                throw new ArgumentException("clientNamePath is required.", nameof(clientNamePath));
            }

            var options = new RabbitConnectionOptions();

            // Load required properties
            var endpointUriValue = configuration.GetValue<string>(endpointUriPath);
            var clientNameValue = configuration.GetValue<string>(clientNamePath);

            if (string.IsNullOrWhiteSpace(endpointUriValue)) {
                throw new ArgumentException($"Configuration value at '{endpointUriPath}' is missing or empty.", nameof(endpointUriPath));
            }

            if (string.IsNullOrWhiteSpace(clientNameValue)) {
                throw new ArgumentException($"Configuration value at '{clientNamePath}' is missing or empty.", nameof(clientNamePath));
            }

            options.EndpointUri = new Uri(endpointUriValue);
            options.ClientName = clientNameValue;

            // Load optional properties (use defaults if not found)
            if (!string.IsNullOrWhiteSpace(automaticRecoveryEnabledPath))
            {
                if (configuration.GetValue<bool?>(automaticRecoveryEnabledPath) is bool automaticRecoveryEnabled)
                {
                    options.AutomaticRecoveryEnabled = automaticRecoveryEnabled;
                }
            }

            if (!string.IsNullOrWhiteSpace(handshakeContinuationTimeoutPath))
            {
                if (configuration.GetValue<int?>(handshakeContinuationTimeoutPath) is int timeoutMs)
                {
                    options.HandshakeContinuationTimeout = TimeSpan.FromMilliseconds(timeoutMs);
                }
            }

            return options;
        }
    }
}

