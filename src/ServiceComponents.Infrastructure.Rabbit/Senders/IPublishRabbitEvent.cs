using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Infrastructure.Rabbit.Senders
{
    public interface IPublishRabbitEvent
    {
        Task PublishAsync<T>(T @event, IBasicProperties basicProperties, CancellationToken cancellationToken = default) where T : IEvent;
    }
}