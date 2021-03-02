using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.AspNet.Http.Senders
{
    public interface IPublishHttpEvent
    {
        Task PublishAsync<T>(T @event, IDictionary<string, string> headers, CancellationToken cancellationToken = default) where T : IEvent;
    }
}