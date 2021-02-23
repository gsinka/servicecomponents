using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Application.Mediator
{
    public interface IPreHandleEvent
    {
        Task PreHandleAsync(IEvent @event, CancellationToken cancellationToken = default);
    }

    public interface IPreHandleEvent<in T> where T : IEvent
    {
        Task PreHandleAsync(T @event, CancellationToken cancellationToken = default);
    }
}