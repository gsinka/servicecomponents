using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Application.Mediator
{
    public interface IPostHandleQuery
    {
        Task PostHandleAsync(IQuery query, object result, CancellationToken cancellationToken = default);
    }

    public interface IPostHandleQuery<in TQuery, TResult> where TQuery : IQuery<TResult>
    {
        Task<TResult> PostHandleAsync(TQuery query, TResult result, CancellationToken cancellationToken = default);
    }
}