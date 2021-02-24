using System.Threading;
using Autofac;
using RabbitMQ.Client;
using ServiceComponents.Infrastructure.Rabbit;

namespace ReferenceApplication.AspNet.Wireup
{
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