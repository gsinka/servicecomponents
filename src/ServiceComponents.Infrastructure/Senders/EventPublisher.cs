using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Senders;

namespace ServiceComponents.Infrastructure.Senders
{
    public class EventPublisher : IPublishEvent
    {
        public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent
        {
            throw new NotImplementedException();
        }

        public Task PublishAsync(IEnumerable<IEvent> events, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}