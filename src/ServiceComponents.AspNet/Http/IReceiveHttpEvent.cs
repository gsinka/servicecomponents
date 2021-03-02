using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.AspNet.Http
{
    public interface IReceiveHttpEvent
    {
        Task ReceiveAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent;
    }
}