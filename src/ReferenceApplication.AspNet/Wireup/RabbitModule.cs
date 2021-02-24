using System;
using Autofac;
using ServiceComponents.Infrastructure.Rabbit;

namespace ReferenceApplication.AspNet.Wireup
{
    public class RabbitModule : Module
    {
        override protected void Load(ContainerBuilder builder)
        {
            builder.AddRabbitConnection(new Uri("amqp://guest:guest@localhost:5672/"), "reference-app");

            builder.AddRabbitChannel(); // Generic channel, for setup for eg.
            builder.RegisterType<RabbitSetup>().AsImplementedInterfaces().InstancePerDependency();

            builder.AddRabbitChannel("publisher"); // Channel dedicated for sender
            builder.AddRabbitEventPublisher("test", string.Empty, channelKey: "publisher", key: "rabbit");

            builder.AddRabbitChannel("consumer1");
            builder.AddRabbitChannel("consumer2");
            builder.AddRabbitConsumer("test", "consumer1", "consumer1");
            builder.AddRabbitConsumer("test", "consumer2", "consumer2");

            builder.AddRabbitReceivers();
            builder.AddRabbitReceiverCorrelationBehavior();

            builder.AddRabbitCommandSender("test", string.Empty, "publisher", "rabbit");
            builder.AddRabbitQuerySender("test", string.Empty, "publisher", "rabbit");

            builder.AddRabbitSenderCorrelationBehavior();
        }
    }
}
