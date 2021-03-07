using System.Threading;
using System.Threading.Tasks;
using Autofac;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application;

namespace ServiceComponents.Infrastructure.Receivers
{
    public class LoopbackQueryReceiver : IReceiveLoopbackQuery
    {
        private readonly ILifetimeScope _scope;

        public LoopbackQueryReceiver(ILifetimeScope scope)
        {
            _scope = scope;
        }

        public async Task<TResult> ReceiveAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
        {
            var correlation = _scope.ResolveOptional<ICorrelation>();
            await using var scope = _scope.BeginLifetimeScope(builder => {
                if (correlation != null) {
                    builder.RegisterInstance(correlation).AsSelf().AsImplementedInterfaces();
                }
            });
            return await scope.Resolve<IReceiveQuery>().ReceiveAsync(query, cancellationToken);

        }
    }
}