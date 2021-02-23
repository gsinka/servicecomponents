using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Core.ExtensionMethods;

namespace ServiceComponents.Infrastructure.Rabbit.Senders
{
    public class RabbitEventPublisher : IPublishRabbitEvent
    {
        private readonly ILogger _log;
        private readonly IModel _model;
        private readonly string _exchange;
        private readonly string _routingKey;
        private readonly bool _mandatory;
        
        public RabbitEventPublisher(ILogger log, IModel model, string exchange, string routingKey, bool mandatory = false)
        {
            _log = log;
            _model = model;
            _exchange = exchange;
            _routingKey = routingKey;
            _mandatory = mandatory;
        }

        public Task PublishAsync<T>(T @event, IBasicProperties basicProperties, CancellationToken cancellationToken = default) where T : IEvent
        {
            _log.ForContext("event", @event, true).Verbose("Publishing {eventType} using RabbitMQ publisher to exchange '{exchange}', routing-key: '{routingKey}'", @event.DisplayName(), _exchange, _routingKey);

            var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event, Formatting.None));
            
            basicProperties.MessageId = @event.EventId;
            basicProperties.Type = @event.AssemblyVersionlessQualifiedName();

            _model.BasicPublish(_exchange, _routingKey, _mandatory, basicProperties, body);

            return Task.CompletedTask;
        }
    }
}
