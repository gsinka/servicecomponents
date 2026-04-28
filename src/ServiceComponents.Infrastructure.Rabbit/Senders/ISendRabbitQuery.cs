using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Infrastructure.Rabbit.Senders
{
    public interface ISendRabbitQuery
    {
        Task<TResult> SendAsync<TResult>(IQuery<TResult> query, BasicProperties basicProperties, CancellationToken cancellationToken = default);
    }
}