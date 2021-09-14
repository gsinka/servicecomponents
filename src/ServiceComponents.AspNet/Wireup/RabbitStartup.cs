using System.Collections.Generic;
using Autofac;
using RabbitMQ.Client;
using Serilog;
using ServiceComponents.Infrastructure.Rabbit;

namespace ServiceComponents.AspNet.Wireup
{
    public class RabbitStartup : IStartable
    {
        private readonly ILogger _log;
        private readonly ILifetimeScope _scope;
        private readonly IModel _channel;
        private readonly string _queue;
        private readonly string _exchange;
        private readonly string _routingKey;
        private readonly int[] _retryIntervals;

        public RabbitStartup(ILogger log, ILifetimeScope scope, IModel channel, string queue, string exchange, string routingKey = "", int[] retryIntervals = default)
        {
            _log = log;
            _scope = scope;
            _channel = channel;
            _queue = queue;
            _exchange = exchange;
            _routingKey = routingKey;
            _retryIntervals = retryIntervals;
        }

        public void Start()
        {
            _channel.ExchangeDeclare(_exchange, "direct", false, true);

            if (_retryIntervals != default) {
                _channel.AddRabbitRetry(_scope, _queue, _retryIntervals);
            }
            else {
                _channel.QueueDeclare(_queue, false, false, true);
            }

            _channel.QueueBind(_queue, _exchange, _routingKey);

            var consumers = _scope.ResolveKeyed<IEnumerable<RabbitConsumer>>("__consumer__");

            foreach (var consumer in consumers) {

                _log.Verbose("Starting consumer consumer-{consumerId}", consumer.ConsumerTag);
                consumer.Start();
            }
        }
    }
}