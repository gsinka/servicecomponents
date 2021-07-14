using System;
using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Application.Mediator
{
    public class EventAwaiterService : IPostHandleEvent
    {
        public EventAwaiterService()
        {
        }

        public Task PostHandleAsync(IEvent @event, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
