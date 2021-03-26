using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application;

namespace ServiceComponents.Infrastructure.Receivers.Loopback
{
    public class LoopbackCommandReceiver : IReceiveLoopbackCommand
    {
        private readonly IReceiveCommand _receiver;

        public LoopbackCommandReceiver(IReceiveCommand receiver)
        {
            _receiver = receiver;
        }

        public async Task ReceiveAsync<TCommand>(TCommand command, ICorrelation correlation, CancellationToken cancellationToken = default) where TCommand : ICommand
        {
            await _receiver.ReceiveAsync(command, cancellationToken);
        }
    }
}