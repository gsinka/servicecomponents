using System.Threading;
using System.Threading.Tasks;
using Autofac;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application;
using ServiceComponents.Infrastructure.Receivers.Loopback;

namespace ServiceComponents.Infrastructure.Senders.Loopback
{
    public class LoopbackQuerySender : ISendLoopbackQuery
    {
        private readonly ILifetimeScope _scope;

        public LoopbackQuerySender(ILifetimeScope scope)
        {
            _scope = scope;
        }

        public async Task<TResult> SendAsync<TResult>(IQuery<TResult> query, ICorrelation correlation, CancellationToken cancellationToken = default)
        {
            await using var scope = _scope.BeginLifetimeScope();
            return await scope.Resolve<IReceiveLoopbackQuery>().ReceiveAsync(query, correlation, cancellationToken);
        }
    }
}
