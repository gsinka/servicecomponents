using System.Threading;
using System.Threading.Tasks;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Senders;

namespace ServiceComponents.Application.Mediator
{
    public abstract class QueryHandler<TQuery, TResult> : IHandleQuery<TQuery, TResult> where TQuery : IQuery<TResult>
    {
        private readonly ISendQuery _querySender;
        protected ILogger Log { get; }
        protected ICorrelation Correlation { get; }

        protected QueryHandler(ILogger log, ICorrelation correlation, ISendQuery querySender)
        {
            _querySender = querySender;
            Log = log;
            Correlation = correlation;
        }

        public abstract Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);

        protected async Task<T> SendAsync<T>(IQuery<T> query, CancellationToken cancellationToken = default)
        {
            return await _querySender.SendAsync(query, cancellationToken);
        }
    }
}