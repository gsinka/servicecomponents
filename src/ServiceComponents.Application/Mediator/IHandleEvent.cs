using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Application.Mediator
{
    public interface IHandleEvent<in T> where T : IEvent
    {
        Task HandleAsync(T @event, CancellationToken cancellationToken = default);
    }
}