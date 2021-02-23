using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Infrastructure.HandlerContext;
using ServiceComponents.Infrastructure.Mediator;
using ServiceComponents.Infrastructure.Mediator.Dispatchers;

namespace ServiceComponents.Infrastructure.Receiver
{
    //public class QueryReceiver : IReceiveQuery
    //{
    //    private readonly IDispatchQuery _queryDispatcher;
    //    private readonly QueryContext _context;

    //    public QueryReceiver(IDispatchQuery queryDispatcher, QueryContext context)
    //    {
    //        _queryDispatcher = queryDispatcher;
    //        _context = context;
    //    }

    //    public async Task<TResult> ReceiveAsync<TResult>(IQuery<TResult> query, string correlationId = null, string causationId = null, string userId = null, string userName = null, CancellationToken cancellationToken = default)
    //    {
    //        if (query != null) _context.Query = query;
    //        if (correlationId != null) _context.CorrelationId = correlationId;
    //        if (causationId != null) _context.CausationId = causationId;
    //        if (userId != null) _context.UserId = userId;
    //        if (userName != null) _context.UserName = userName;

    //        return await _queryDispatcher.DispatchAsync(query, cancellationToken);
    //    }
    //}
}