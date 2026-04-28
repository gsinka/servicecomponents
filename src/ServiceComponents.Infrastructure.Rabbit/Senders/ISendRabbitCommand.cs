using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Infrastructure.Rabbit.Senders
{
    public interface ISendRabbitCommand
    {
        Task SendAsync<T>(T command, BasicProperties basicProperties, CancellationToken cancellationToken = default) where T : ICommand;
    }
}