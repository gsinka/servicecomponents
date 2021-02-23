using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Infrastructure.Receiver
{
    public interface IReceiveQuery
    {
        Task<TResult> ReceiveAsync<TResult>(IQuery<TResult> query, string correlationId = null, string causationId = null, string userId = null, string userName = null, CancellationToken cancellationToken = default);
    }
}