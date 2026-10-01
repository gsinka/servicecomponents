using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;

namespace ServiceComponents.Infrastructure.Rabbit
{
    public interface IReceiveRabbitMessage
    {
        Task ReceiveAsync(string body, BasicDeliverEventArgs args, CancellationToken cancellationToken);
    }
}