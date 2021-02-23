using System;
using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Application.Mediator
{
    public interface IHandleQueryFailure
    {
        Task HandleFailureAsync(IQuery query, Exception exception, CancellationToken cancellationToken = default);
    }

    public interface IHandleQueryFailure<in TQuery, TResult> where TQuery : IQuery<TResult>
    {
        Task HandleFailureAsync(TQuery query, Exception exception, CancellationToken cancellationToken = default);
    }
}