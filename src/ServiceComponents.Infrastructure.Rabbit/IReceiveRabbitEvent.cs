using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Infrastructure.Rabbit
{
    public interface IReceiveRabbitEvent
    {
        Task ReceiveAsync<T>(T @event, BasicDeliverEventArgs args, CancellationToken cancellationToken) where T : IEvent;
    }
}