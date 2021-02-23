using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Infrastructure.Dispatchers
{
    public interface IDispatchEvent
    {
        Task DispatchAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IEvent;
    }
}