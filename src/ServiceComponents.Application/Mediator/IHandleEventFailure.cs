using System;
using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Application.Mediator
{
    public interface IHandleEventFailure
    {
        Task HandleFailureAsync(IEvent @event, Exception exception, CancellationToken cancellationToken = default);
    }

    public interface IHandleEventFailure<in T> where T : IEvent
    {
        Task HandleFailureAsync(T @event, Exception exception, CancellationToken cancellationToken = default);
    }
}