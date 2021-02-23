using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Application.Mediator
{
    public interface IPreHandleQuery
    {
        Task PreHandleAsync(IQuery query, CancellationToken cancellationToken = default);
    }

    public interface IPreHandleQuery<in TQuery, TResult> where TQuery : IQuery<TResult>
    {
        Task PreHandleAsync(TQuery query, CancellationToken cancellationToken = default);
    }
}