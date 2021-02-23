using System;
using System.Threading;
using Autofac;
using Autofac.Features.Indexed;
using RabbitMQ.Client;
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

    public class RabbitSetup : IStartable
    {
        private readonly ILifetimeScope _scope;
        private readonly IModel _channel;

        public RabbitSetup(ILifetimeScope scope, IModel channel)
        {
            _scope = scope;
            _channel = channel;
        }

        public void Start()
        {
            _channel.ExchangeDeclare("test", "fanout", false, true);
            _channel.QueueDeclare("test", false, false, true);
            _channel.QueueBind("test", "test", string.Empty);

            //_scope.Resolve<RabbitConsumer>().StartAsync(CancellationToken.None).Wait();
            _scope.ResolveKeyed<RabbitConsumer>("consumer1").StartAsync(CancellationToken.None).Wait();
            _scope.ResolveKeyed<RabbitConsumer>("consumer2").StartAsync(CancellationToken.None).Wait();

        }
    }
}
