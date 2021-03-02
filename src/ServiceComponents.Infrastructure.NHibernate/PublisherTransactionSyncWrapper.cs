using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NHibernate;
using NHibernate.Transaction;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Senders;
using ServiceComponents.Core;

namespace ServiceComponents.Infrastructure.NHibernate
{
    internal class PublisherTransactionSyncWrapper : IPublishEvent, ITransactionCompletionSynchronization
    {
        private readonly ILogger _log;
        private readonly IPublishEvent _next;
        private readonly ISession _session;
        private readonly IClock _clock;
        private readonly ICollection<(IEvent @event, OutgoingEventEntity entity)> _unpublishedEvents = new List<(IEvent @event, OutgoingEventEntity entity)>();

        public PublisherTransactionSyncWrapper(ILogger log, IPublishEvent next, ISession session, IClock clock)
        {
            _log = log;
            _next = next;
            _session = session;
            _clock = clock;
        }

        public async Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : IEvent
        {
            var outgoingEvent = new OutgoingEventEntity() {
                
                EventId = @event.EventId,
                EventType = @event.GetType().AssemblyQualifiedName,
                EventPayload = JsonConvert.SerializeObject(@event, Formatting.None),
                TimeStamp = _clock.UtcNow,
                RetryCount = 0
            };

            _unpublishedEvents.Add((@event, outgoingEvent));
            await _session.SaveAsync(outgoingEvent, cancellationToken);
        }

        public async Task PublishAsync(IEnumerable<IEvent> events, CancellationToken cancellationToken = default)
        {
            foreach (IEvent @event in events) {
                await PublishAsync(@event, cancellationToken);
            }
        }

        public void ExecuteBeforeTransactionCompletion() { }

        public Task ExecuteBeforeTransactionCompletionAsync(CancellationToken cancellationToken) { return Task.CompletedTask; }

        public void ExecuteAfterTransactionCompletion(bool success) { ExecuteAfterTransactionCompletionAsync(success, CancellationToken.None).Wait(); }

        public async Task ExecuteAfterTransactionCompletionAsync(bool success, CancellationToken cancellationToken)
        {
            if (success) {

                using var tx = _session.BeginTransaction();

                try {
                    
                    await _next.PublishAsync(_unpublishedEvents.Select(x => x.@event), cancellationToken);
                    
                    foreach (var @event in _unpublishedEvents) {
                        await _session.DeleteAsync(@event.entity, cancellationToken);
                    }

                    _log.Verbose("{eventCount} published event(s) deleted from transaction", _unpublishedEvents.Count);

                    await tx.CommitAsync(cancellationToken);
                }
                catch (Exception exception) {

                    await tx.RollbackAsync(cancellationToken);
                    
                    _log.Verbose("Failed publishing {eventCount} event(s) from transaction, notifying crawler service", _unpublishedEvents.Count);
                }
            }

            _unpublishedEvents.Clear();
        }
    }
}