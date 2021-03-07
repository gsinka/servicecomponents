using System.Threading;
using System.Threading.Tasks;
using Autofac;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application;

namespace ServiceComponents.Infrastructure.Receivers
{
    public class LoopbackEventReceiver : IReceiveLoopbackEvent
    {
        private readonly ILifetimeScope _scope;

        public LoopbackEventReceiver(ILifetimeScope scope)
        {
            _scope = scope;
        }

        public async Task ReceiveAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IEvent
        {
            var correlation = _scope.ResolveOptional<ICorrelation>();
            await using var scope = _scope.BeginLifetimeScope(builder => {
                if (correlation != null) {
                    builder.RegisterInstance(correlation).AsSelf().AsImplementedInterfaces();
                }
            });
            await scope.Resolve<IReceiveEvent>().ReceiveAsync(@event, cancellationToken);
        }
    }
}