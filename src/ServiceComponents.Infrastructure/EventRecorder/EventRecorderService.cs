using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application;
using ServiceComponents.Core.Services;

namespace ServiceComponents.Infrastructure.EventRecorder
{
    /// <summary>
    /// 
    /// </summary>
    public class EventRecorderService : IEventRecorder
    {
        private readonly ILogger _log;
        private readonly IClock _clock;
        private readonly int _eventTtl;
        private readonly object _lockObj = new ();
        private readonly List<RecordedEvent> _events = new();
        private readonly IList<WaitTask> _tasks = new List<WaitTask>();

        /// <summary>
        /// 
        /// </summary>
        public EventRecorderService(ILogger log, IClock clock, int eventTTL = 60000)
        {
            _log = log;
            _clock = clock;
            _eventTtl = eventTTL;
        }

        public async Task<T> WaitFor<T>(Func<T, ICorrelation, bool> filter, TimeSpan timeOut) where T : IEvent
        {
            using var cancellationTokenSource = new CancellationTokenSource(timeOut);
            
            return await WaitFor<T>(filter, cancellationTokenSource.Token);
        }

        public Task<T> WaitFor<T>(Func<T, ICorrelation, bool> filter, CancellationToken cancellationToken = default) where T : IEvent
        {
            RemoveExpiredEvents();

            lock (_lockObj) {
                
                var matchingRecordedEvent = _events
                    .Where(x => x.Event is T)
                    .FirstOrDefault(recordedEvent => filter((T)recordedEvent.Event, recordedEvent.Correlation));

                if (matchingRecordedEvent != null) {
                    _log.Debug("Matching event for {eventType} returned from event store with age of {eventAge} ms", typeof(T).FullName, (_clock.UtcNow - matchingRecordedEvent.TimeStamp).TotalMilliseconds);
                    return Task.FromResult((T)matchingRecordedEvent.Event);
                }
            }

            var task = new WaitTask((evnt, args) => filter((T)evnt, args), typeof(T));

            cancellationToken.Register(() => {
                _log.Debug("Awaiting event on task {awaiterTaskId} cancelled for event {eventType}", task.GetHashCode(), typeof(T).FullName);
                task.WaitHandle.Set();
            });

            _log.Debug("Creating event awaiter task {awaiterTaskId} for event {eventType}", task.GetHashCode(), typeof(T).FullName);

            lock (_lockObj) { _tasks.Add(task); }

            return new TaskFactory<T>().StartNew(() => {
                task.WaitHandle.WaitOne();

                _log.Verbose("Removing awaiting event task {awaiterTaskId} for event {eventType}", task.GetHashCode(), typeof(T).FullName);

                lock (_lockObj) { _tasks.Remove(task); }

                return (T)task.Event;

            }, cancellationToken);
        }

        private class WaitTask
        {
            public Func<IEvent, ICorrelation, bool> EventFilter { get; }
            public Type EventType { get; }
            public EventWaitHandle WaitHandle { get; }
            public IEvent Event { get; set; }

            public WaitTask(Func<IEvent, ICorrelation, bool> eventFilter, Type eventType)
            {
                EventFilter = eventFilter;
                EventType = eventType;
                WaitHandle = new ManualResetEvent(false);
            }
        }

        internal Task PreHandleEvent(IEvent @event, ICorrelation correlation, CancellationToken cancellationToken = default)
        {
            RemoveExpiredEvents();

            _log.Debug("Recording event {eventType} in event store", @event.GetType().FullName);

            lock (_lockObj) {
                _events.Add(new RecordedEvent(_clock.UtcNow, @event, correlation));
            }

            return Task.Run(() => {
                _log.Debug("Processing event awaiters for event {eventType}", @event.GetType().FullName);
                _log.Verbose("Checking {awaiterCount} awaiter(s)", _tasks.Count);

                lock (_lockObj) {
                    foreach (var task in _tasks.Where(x => @event.GetType() == x.EventType && x.EventFilter(@event, correlation))) {
                        _log.Verbose("Event awaiter task {awaiterTaskId} matching event {eventType}", task.GetHashCode(), @event.GetType().FullName);

                        task.Event = @event;
                        task.WaitHandle.Set();
                    }
                }
            }, cancellationToken);
        }

        public IEnumerable<RecordedEvent> RecordedEvents {
            get { lock (_lockObj) { return _events.ToArray(); } }
        }

        private void RemoveExpiredEvents()
        {
            lock (_lockObj) {
                var maxTime = _clock.UtcNow.AddMilliseconds(-_eventTtl);
                foreach (var oldEvent in _events.Where(recordedEvent => recordedEvent.TimeStamp < maxTime).ToList()) {
                    _log.Debug("Removing expired event {eventType} with age of {eventAge} from event store", oldEvent.Event.GetType().FullName, (_clock.UtcNow - oldEvent.TimeStamp).TotalMilliseconds);
                    _events.Remove(oldEvent);
                }
            }
        }

        public Task ClearRecordedEvents(CancellationToken cancellationToken = default)
        {
            lock (_lockObj) {
                _events.Clear();
            }

            return Task.CompletedTask;
        }

    }
}
