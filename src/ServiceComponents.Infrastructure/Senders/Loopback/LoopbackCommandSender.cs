using System.Threading;
using System.Threading.Tasks;
using Autofac;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application;
using ServiceComponents.Infrastructure.Receivers;

namespace ServiceComponents.Infrastructure.Senders
{
    public class LoopbackCommandSender : ISendLoopbackCommand
    {
        private readonly ILifetimeScope _scope;
        
        public LoopbackCommandSender(ILifetimeScope scope)
        {
            _scope = scope;
        }

        public async Task SendAsync<TCommand>(TCommand command, ICorrelation correlation, CancellationToken cancellationToken = default) where TCommand : ICommand
        {
            await using var scope = _scope.BeginLifetimeScope();
            await scope.Resolve<IReceiveLoopbackCommand>().ReceiveAsync(command, correlation, cancellationToken);
        }
    }
}