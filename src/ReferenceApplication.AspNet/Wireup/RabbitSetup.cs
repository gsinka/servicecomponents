using System;
using System.Threading;
using Autofac;
using RabbitMQ.Client;
using Serilog;
using ServiceComponents.Infrastructure.Rabbit;

namespace ReferenceApplication.AspNet.Wireup
{
    public class RabbitSetup : IStartable
    {
        private readonly ILogger _log;
        private readonly ILifetimeScope _scope;
        private readonly IModel _channel;

        public RabbitSetup(ILogger log, ILifetimeScope scope, IModel channel)
        {
            _log = log;
            _scope = scope;
            _channel = channel;
        }

        public void Start()
        {
            _channel.ExchangeDeclare("test", "fanout", false, true);
            _channel.QueueDeclare("test", false, false, true);
            _channel.QueueBind("test", "test", string.Empty);
            
            for (var i = 0; i < Environment.ProcessorCount; i++) {
                _log.Verbose("Starting consumer consumer-{consumerId}", i);
                _scope.ResolveKeyed<RabbitConsumer>($"consumer-{i}").StartAsync(CancellationToken.None).Wait();
            }
        }
    }
}