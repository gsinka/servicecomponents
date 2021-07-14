using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application;

namespace ServiceComponents.Infrastructure.Rabbit.Senders
{
    public class RabbitEventSenderCorrelationBehavior : RabbitSenderCorrelationBehavior, IPublishRabbitEvent
    {
        private readonly IPublishRabbitEvent _next;

        public RabbitEventSenderCorrelationBehavior(ILogger log, ICorrelation correlation, IPublishRabbitEvent next) : base(log, correlation)
        {
            _next = next;
        }

        public async Task PublishAsync<T>(T @event, IBasicProperties basicProperties, CancellationToken cancellationToken = default) where T : IEvent
        {
            UpdateCorrelation(basicProperties);
            await _next.PublishAsync(@event, basicProperties, cancellationToken);
        }
    }
}