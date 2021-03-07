using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Infrastructure.Receivers;

namespace ServiceComponents.Infrastructure.Senders
{
    public class LoopbackCommandSender : ISendLoopbackCommand
    {
        private readonly IReceiveLoopbackCommand _receiver;
        
        public LoopbackCommandSender(IReceiveLoopbackCommand receiver)
        {
            _receiver = receiver;
        }

        public async Task SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand
        {
            await _receiver.ReceiveAsync(command, cancellationToken);
        }
    }
}