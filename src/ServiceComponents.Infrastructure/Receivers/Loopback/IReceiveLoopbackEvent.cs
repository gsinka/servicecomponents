using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application;

namespace ServiceComponents.Infrastructure.Receivers.Loopback
{
    public interface IReceiveLoopbackEvent
    {
        Task ReceiveAsync<T>(T @event, ICorrelation correlation, CancellationToken cancellationToken = default) where T : IEvent;
    }
}