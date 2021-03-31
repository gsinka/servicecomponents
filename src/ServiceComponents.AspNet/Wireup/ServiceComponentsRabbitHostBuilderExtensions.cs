using System;
using Autofac;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using Serilog;
using ServiceComponents.Infrastructure.Rabbit;

namespace ServiceComponents.AspNet.Wireup
{
    public static class ServiceComponentsRabbitHostBuilderExtensions
    {
        public static ServiceComponentsHostBuilder AddRabbit(
            this ServiceComponentsHostBuilder hostBuilder, 
            Func<IConfiguration, string> connectionStringBuilder,
            Func<IConfiguration, string> clientNameBuilder,
            Func<IConfiguration, string> queueBuilder,
            Func<IConfiguration, string> exchangeBuilder,
            Func<IConfiguration, string> routingKeyBuilder = default,
            Func<IConfiguration, int[]> retryIntervalsBuilder = default)
        {

            return hostBuilder.RegisterCallback((context, containerBuilder) => {

                var uri = new Uri(connectionStringBuilder(context.Configuration));
                var clientName = clientNameBuilder(context.Configuration);
                var exchange = exchangeBuilder(context.Configuration);
                var queue = queueBuilder(context.Configuration);
                var routingKey = routingKeyBuilder(context.Configuration) ?? "";
                var retryIntervals = retryIntervalsBuilder(context.Configuration);

                // Generic connection and channel

                containerBuilder.AddRabbitConnection(uri, $"{clientName}-generic");
                containerBuilder.AddRabbitChannel();

                // Publisher connection, channel

                containerBuilder.AddRabbitConnection(uri, $"{clientName}-publisher", "publisher");
                containerBuilder.AddRabbitChannel(key: "publisher", connectionKey: "publisher");
                containerBuilder.AddRabbitEventPublisher(exchange, routingKey, channelKey: "publisher", key: "rabbit");

                // Consumers

                containerBuilder.AddRabbitConnection(uri, $"{clientName}-consumer", "consumer");

                // Add retry if defined
                if (retryIntervals != default) {
                    containerBuilder.AddRabbitRetryConsumers("consumer", queue, retryIntervals, clientName);
                }

                // Add consumers for queue
                var consumerCount = Environment.ProcessorCount - (retryIntervals?.Length ?? 0);
                if (consumerCount < 1) consumerCount = 1;

                for (var i = 0; i < consumerCount; i++) {

                    containerBuilder.AddRabbitChannel(connectionKey: "consumer", key: $"consumer-{i}");
                    containerBuilder.AddRabbitConsumer(queue, $"{clientName}-consumer-{i}", $"consumer-{i}", $"consumer-{i}");
                }

                // Receivers and senders

                containerBuilder.AddRabbitReceivers();
                containerBuilder.AddRabbitCommandSender(exchange, string.Empty, "publisher", "rabbit");
                containerBuilder.AddRabbitQuerySender(exchange, string.Empty, "publisher", "rabbit");

                containerBuilder.Register(context => new RabbitStartup(
                    context.Resolve<ILogger>(),
                    context.Resolve<ILifetimeScope>(),
                    context.Resolve<IModel>(),
                    queue, exchange, routingKey,
                    retryIntervals)).AsImplementedInterfaces();

            });

        }

        public static ServiceComponentsHostBuilder AddRabbit(this ServiceComponentsHostBuilder hostBuilder, string connectionString, string clientName, string queue, string exchange, string routingKey = "", int[] retryIntervals = default)
        {
            return AddRabbit(hostBuilder, configuration => connectionString, configuration => clientName,
                configuration => queue, configuration => exchange, configuration => routingKey,
                configuration => retryIntervals);
        }
    }
}