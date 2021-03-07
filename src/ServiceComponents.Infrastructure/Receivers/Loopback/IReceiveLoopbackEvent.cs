using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Infrastructure.Receivers
{
    public interface IReceiveLoopbackEvent
    {
        Task ReceiveAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IEvent;
    }
}