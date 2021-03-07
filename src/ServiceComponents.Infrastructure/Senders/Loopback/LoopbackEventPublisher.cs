using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Senders;
using ServiceComponents.Infrastructure.Receivers;

namespace ServiceComponents.Infrastructure.Senders
{
    public class LoopbackEventPublisher : IPublishLoopbackEvent
    {
        private readonly IReceiveLoopbackEvent _receiver;

        public LoopbackEventPublisher(IReceiveLoopbackEvent receiver)
        {
            _receiver = receiver;
        }

        public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent
        {
            await _receiver.ReceiveAsync(@event, cancellationToken);
        }

        public async Task PublishAsync(IEnumerable<IEvent> events, CancellationToken cancellationToken = default)
        {
            foreach (IEvent @event in events) {
                await PublishAsync(@event, cancellationToken);
            }
        }
    }
}