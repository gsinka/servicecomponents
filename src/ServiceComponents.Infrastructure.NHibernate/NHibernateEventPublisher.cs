using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NHibernate;
using RabbitMQ.Client;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Core;
using ServiceComponents.Infrastructure.Rabbit.Senders;

namespace ServiceComponents.Infrastructure.NHibernate
{
    //public class NHibernateEventPublisher : IPublishRabbitEvent
    //{
    //    private readonly IClock _clock;
    //    private readonly ISession _session;
    //    private readonly Channel<OutgoingEventEntity> _channel;
    //    private readonly IPublishRabbitEvent _next;

    //    public NHibernateEventPublisher(IClock clock, ISession session, Channel<OutgoingEventEntity> channel, IPublishRabbitEvent next)
    //    {
    //        _clock = clock;
    //        _session = session;
    //        _channel = channel;
    //        _next = next;
    //    }

    //    public async Task PublishAsync<T>(T @event, IBasicProperties basicProperties, CancellationToken cancellationToken = default) where T : IEvent
    //    {
    //        try {

    //            var outgoingEventEntity = await _session.GetAsync<OutgoingEventEntity>(@event.EventId, cancellationToken);

    //            if (outgoingEventEntity == null) {
                
    //                await _session.SaveAsync(new OutgoingEventEntity() {
    //                    EventId = @event.EventId,
    //                    EventType = @event.GetType().AssemblyQualifiedName,
    //                    EventPayload = JsonConvert.SerializeObject(@event, Formatting.None),
    //                    TimeStamp = _clock.UtcNow,
    //                    RetryCount = 0
    //                }, cancellationToken);
    //            }
    //            else {

    //                outgoingEventEntity.RetryCount++;
    //                await _session.UpdateAsync(outgoingEventEntity, cancellationToken);
    //            }
    //        }
    //        catch (Exception exception) {
    //            throw new InvalidOperationException("Storing published event failed", exception);
    //        }
    //    }
    //}
}

