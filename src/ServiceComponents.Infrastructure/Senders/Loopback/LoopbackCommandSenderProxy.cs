using System.Threading;
using System.Threading.Tasks;
using Autofac;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application;
using ServiceComponents.Application.Senders;

namespace ServiceComponents.Infrastructure.Senders.Loopback
{
    public class LoopbackCommandSenderProxy : ISendCommand
    {
        private readonly ILifetimeScope _scope;
        private readonly ISendLoopbackCommand _next;

        public LoopbackCommandSenderProxy(ILifetimeScope scope, ISendLoopbackCommand next)
        {
            _scope = scope;
            _next = next;
        }

        public async Task SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand
        {
            var correlation = _scope.ResolveOptional<ICorrelation>();
            await _next.SendAsync(command, correlation, cancellationToken);
        }
    }
}