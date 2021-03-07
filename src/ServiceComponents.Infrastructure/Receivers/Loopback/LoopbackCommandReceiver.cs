using System.Threading;
using System.Threading.Tasks;
using Autofac;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application;

namespace ServiceComponents.Infrastructure.Receivers
{
    public class LoopbackCommandReceiver : IReceiveLoopbackCommand
    {
        private readonly ILifetimeScope _scope;

        public LoopbackCommandReceiver(ILifetimeScope scope)
        {
            _scope = scope;
        }

        public async Task ReceiveAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand
        {
            var correlation = _scope.ResolveOptional<ICorrelation>();
            await using var scope = _scope.BeginLifetimeScope(builder => {
                if (correlation != null) {
                    builder.RegisterInstance(correlation).AsSelf().AsImplementedInterfaces();
                }
            });
            await scope.Resolve<IReceiveCommand>().ReceiveAsync(command, cancellationToken);
        }
    }
}