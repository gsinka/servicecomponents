using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.AspNet.Http
{
    public interface IReceiveHttpQuery
    {
        Task<TResult> ReceiveAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);
    }
}