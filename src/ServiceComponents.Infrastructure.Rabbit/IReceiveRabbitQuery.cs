using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Infrastructure.Rabbit
{
    public interface IReceiveRabbitQuery
    {
        Task<T> ReceiveAsync<T>(IQuery<T> query, BasicDeliverEventArgs args, CancellationToken cancellationToken);
    }
}