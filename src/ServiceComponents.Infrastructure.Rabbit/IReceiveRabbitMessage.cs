using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Infrastructure.Rabbit
{
    public interface IReceiveRabbitMessage
    {
        Task ReceiveAsync(string body, BasicDeliverEventArgs args, CancellationToken cancellationToken);
    }
}