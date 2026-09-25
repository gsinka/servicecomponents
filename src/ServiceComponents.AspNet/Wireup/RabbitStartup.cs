using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
        private readonly IChannel _channel;
        private readonly string _queue;
        private readonly string _exchange;
        private readonly string _routingKey;
        private readonly int[] _retryIntervals;

        public RabbitStartup(ILogger log, ILifetimeScope scope, IChannel channel, string queue, string exchange, string routingKey = "", int[] retryIntervals = default)
        {
            _log = log;
            _scope = scope;
            _channel = channel;
            _queue = queue;
            _exchange = exchange;
            _routingKey = routingKey;
            _retryIntervals = retryIntervals;
        }

        /// <summary>
        /// Starts the RabbitMQ initialization.
        /// Use StartAsync for async/await support.
        /// </summary>
        [Obsolete("Use StartAsync for async/await support. This method is maintained for backward compatibility.")]
        public void Start()
        {
            _channel.ExchangeDeclareAsync(_exchange, "direct", false, true).GetAwaiter().GetResult();

            if (_retryIntervals != default) {
                _channel.AddRabbitRetry(_scope, _queue, _retryIntervals);
            }
            else {
                _channel.QueueDeclareAsync(_queue, false, false, true).GetAwaiter().GetResult();
            }

            _channel.QueueBindAsync(_queue, _exchange, _routingKey).GetAwaiter().GetResult();

            var consumers = _scope.ResolveKeyed<IEnumerable<RabbitConsumer>>("__consumer__");

            foreach (var consumer in consumers) {

                _log.Verbose("Starting consumer consumer-{consumerId}", consumer.ConsumerTag);
                consumer.StartAsync().GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// Starts the RabbitMQ initialization with async/await support.
        /// </summary>
        public async Task StartAsync()
        {
            await _channel.ExchangeDeclareAsync(_exchange, "direct", false, true);

            if (_retryIntervals != default) {
                await _channel.AddRabbitRetryAsync(_scope, _queue, _retryIntervals);
            }
            else {
                await _channel.QueueDeclareAsync(_queue, false, false, true);
            }

            await _channel.QueueBindAsync(_queue, _exchange, _routingKey);

            var consumers = _scope.ResolveKeyed<IEnumerable<RabbitConsumer>>("__consumer__");

            foreach (var consumer in consumers) {

                _log.Verbose("Starting consumer consumer-{consumerId}", consumer.ConsumerTag);
                await consumer.StartAsync();
            }
        }
    }
}
