using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Application.Mediator
{
    public interface IPostHandleEvent
    {
        Task PostHandleAsync(IEvent @event, CancellationToken cancellationToken = default);
    }

    public interface IPostHandleEvent<in T> where T : IEvent
    {
        Task PostHandleAsync(T @event, CancellationToken cancellationToken = default);
    }
}