using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NHibernate;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Senders;
using ServiceComponents.Core.Services;

namespace ServiceComponents.Infrastructure.NHibernate
{
    public class NHibernateEventPublisherBehavior : IPublishEvent, IDisposable
    {
        private readonly ISession _session;
        private readonly IClock _clock;
        private readonly IPublishEvent _next;
        private readonly IList<(IEvent evnt, IDictionary<string, string> args)> _unpublishedEvents = new List<(IEvent evnt, IDictionary<string, string> args)>();

        public NHibernateEventPublisherBehavior(ISession session, IClock clock, IPublishEvent next)
        {
            _session = session;
            _clock = clock;
            _next = next;
        }

        public async Task PublishAsync<TEvent>(TEvent @event, IDictionary<string, string> args = default, CancellationToken cancellationToken = default) where TEvent : IEvent
        {
            await _session.SaveAsync(new UnpublishedEventEntity() {
         
                TimeStamp = _clock.UtcNow,
                EventType = @event.GetType().ToString(),
                EventBody = JsonConvert.SerializeObject(@event, new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All })

            }, cancellationToken);

            _unpublishedEvents.Add((@event, args));
        }

        public async Task PublishAsync(IEnumerable<IEvent> events, IDictionary<string, string> args = default, CancellationToken cancellationToken = default)
        {
            foreach (var @event in events) {
                await PublishAsync(@event, args, cancellationToken);
            }
        }

        public void Dispose()
        {
            foreach ((IEvent evnt, IDictionary<string, string> args) unpublishedEvent in _unpublishedEvents) {
                _next.PublishAsync(unpublishedEvent.evnt, unpublishedEvent.args, CancellationToken.None).Wait();
            }
        }
    }

    public class UnpublishedEventEntity
    {
        public virtual long Id { get; set; }
        public virtual DateTime TimeStamp { get; set; }
        public virtual string EventType { get; set; }
        public virtual string EventBody { get; set; }
    }
}
