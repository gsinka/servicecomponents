using System;
using System.Linq;
using Autofac;
using Microsoft.Extensions.Configuration;
using ServiceComponents.Infrastructure.NHibernate;
using ServiceComponents.Infrastructure.Rabbit;

namespace ReferenceApplication.AspNet.Wireup
{
    public class RabbitModule : Module
    {
        private readonly IConfiguration _configuration;

        public RabbitModule(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        override protected void Load(ContainerBuilder builder)
        {
            var queue = "test";
            var exchange = "test";
            var routingKey = string.Empty;
            var clientName = _configuration.GetValue<string>("rabbitMQ:clientName");

            // Generic connection and channel
            builder.AddRabbitConnection(new Uri(_configuration.GetValue<string>("rabbitMQ:endpointUri")), $"{clientName}-generic");
            builder.AddRabbitChannel();
            
            // Publisher connection, channel
            builder.AddRabbitConnection(new Uri(_configuration.GetValue<string>("rabbitMQ:endpointUri")), $"{clientName}-publisher", "publisher");
            builder.AddRabbitChannel(key: "publisher", connectionKey: "publisher");
            builder.AddRabbitEventPublisher(exchange, routingKey, channelKey: "publisher", key: "rabbit");
            //builder.AddNhibernateRabbitPublisher("rabbit-nhibernate");

            // Consumers
            builder.AddRabbitConnection(new Uri(_configuration.GetValue<string>("rabbitMQ:endpointUri")), $"{clientName}-consumer", "consumer");

            for (var i = 0; i < Environment.ProcessorCount; i++) {

                builder.AddRabbitChannel(connectionKey: "consumer", key: $"consumer-{i}");
                builder.AddRabbitConsumer(queue, $"{clientName}-consumer-{i}", $"consumer-{i}", $"consumer-{i}");
            }

            var ttls = _configuration.GetValue("rabbit:retryIntervals", "1000, 3000, 10000")
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse)
                .OrderBy(i => i)
                .ToArray();


            builder.AddRabbitRetryConsumers("consumer", queue, ttls, clientName);

            //builder.AddRabbitChannel(connectionKey: "consumer", key: $"consumer-retry");
            //builder.AddRabbitConsumer("r1", $"{clientName}-consumer-r1", $"consumer-retry", $"consumer-r1");
            ////builder.AddRabbitConsumer("r2", $"{clientName}-consumer-r2", $"consumer-retry", $"consumer-r2");
            ////builder.AddRabbitConsumer("r3", $"{clientName}-consumer-r3", $"consumer-retry", $"consumer-r3");

            builder.AddRabbitReceivers();
            builder.AddRabbitReceiverCorrelationBehavior();

            builder.AddRabbitCommandSender(exchange, string.Empty, "publisher", "rabbit");
            builder.AddRabbitQuerySender(exchange, string.Empty, "publisher", "rabbit");

            builder.AddRabbitSenderCorrelationBehavior();

            // Setup - uses generic connection
            builder.RegisterType<RabbitSetup>().AsImplementedInterfaces().InstancePerDependency();
        }
    }
}
