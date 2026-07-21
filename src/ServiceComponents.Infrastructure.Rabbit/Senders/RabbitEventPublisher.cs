using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Core;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Core.Extensions;

namespace ServiceComponents.Infrastructure.Rabbit.Senders
{
    public class RabbitEventPublisher : IPublishRabbitEvent
    {
        private readonly ILogger _log;
        private readonly IChannel _channel;
        private readonly string _exchange;
        private readonly string _routingKey;
        private readonly bool _mandatory;

        public RabbitEventPublisher(ILogger log, IChannel channel, string exchange, string routingKey, bool mandatory = false)
        {
            _log = log;
            _channel = channel;
            _exchange = exchange;
            _routingKey = routingKey;
            _mandatory = mandatory;
        }

        public async Task PublishAsync<T>(T @event, BasicProperties basicProperties, CancellationToken cancellationToken = default) where T : IEvent
        {
            var routableEvent = (@event as RoutableEvent);
            var evnt = routableEvent?.Event ?? @event;
            var routingKey = routableEvent?.RoutingKey ?? _routingKey;

            _log.ForContext("event", evnt, true).Verbose("Publishing {eventType} using RabbitMQ publisher to exchange '{exchange}', routing-key: '{routingKey}'", @event.DisplayName(), _exchange, routingKey);

            var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(evnt, Formatting.None));

            basicProperties.MessageId = @event.EventId;
            basicProperties.Type = evnt.AssemblyVersionlessQualifiedName();

            await _channel.BasicPublishAsync(_exchange, routingKey, _mandatory, basicProperties, body, cancellationToken);
        }
    }
}
