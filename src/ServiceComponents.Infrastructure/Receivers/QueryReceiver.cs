using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Infrastructure.Dispatchers;

namespace ServiceComponents.Infrastructure.Receivers
{
    public class QueryReceiver : IReceiveQuery
    {
        private readonly IDispatchQuery _queryDispatcher;

        public QueryReceiver(IDispatchQuery queryDispatcher)
        {
            _queryDispatcher = queryDispatcher;
        }
        public async Task<TResult> ReceiveAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
        {
            return await _queryDispatcher.DispatchAsync(query, cancellationToken);
        }
    }
}