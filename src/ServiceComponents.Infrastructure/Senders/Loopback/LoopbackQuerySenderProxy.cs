using System.Threading;
using System.Threading.Tasks;
using Autofac;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application;
using ServiceComponents.Application.Senders;

namespace ServiceComponents.Infrastructure.Senders.Loopback
{
    public class LoopbackQuerySenderProxy : ISendQuery
    {
        private readonly ILifetimeScope _scope;
        private readonly ISendLoopbackQuery _next;

        public LoopbackQuerySenderProxy(ILifetimeScope scope, ISendLoopbackQuery next)
        {
            _scope = scope;
            _next = next;
        }

        public async Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
        {
            var correlation = _scope.ResolveOptional<ICorrelation>();

            return await _next.SendAsync(query, correlation, cancellationToken);
        }
    }
}