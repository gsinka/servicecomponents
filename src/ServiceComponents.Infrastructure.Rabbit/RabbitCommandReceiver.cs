using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Infrastructure.Receivers;

namespace ServiceComponents.Infrastructure.Rabbit
{
    public class RabbitCommandReceiver : IReceiveRabbitCommand
    {
        private readonly IReceiveCommand _commandReceiver;

        public RabbitCommandReceiver(IReceiveCommand commandReceiver)
        {
            _commandReceiver = commandReceiver;
        }

        public async Task ReceiveAsync<T>(T command, BasicDeliverEventArgs args, CancellationToken cancellationToken) where T : ICommand
        {
            await _commandReceiver.ReceiveAsync(command, cancellationToken);
        }
    }
}