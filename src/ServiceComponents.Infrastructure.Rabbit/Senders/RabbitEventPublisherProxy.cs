using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Senders;

namespace ServiceComponents.Infrastructure.Rabbit.Senders
{
    public class RabbitEventPublisherProxy : IPublishEvent
    {
        private readonly IModel _model;
        private readonly IPublishRabbitEvent _rabbitSender;

        public RabbitEventPublisherProxy(IModel model, IPublishRabbitEvent rabbitSender)
        {
            _model = model;
            _rabbitSender = rabbitSender;
        }

        public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent
        {
            var basicProperties = _model.CreateBasicProperties();
            basicProperties.Headers = new Dictionary<string, object>();

            await _rabbitSender.PublishAsync(@event, basicProperties, cancellationToken);

        }

        public async Task PublishAsync(IEnumerable<IEvent> events, CancellationToken cancellationToken = default)
        {
            foreach (var @event in events)
            {
                await PublishAsync(@events, cancellationToken);
            }
        }
    }
}