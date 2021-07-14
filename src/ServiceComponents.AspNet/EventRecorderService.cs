using System;
using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application;
using ServiceComponents.Application.Mediator;
using ServiceComponents.Application.Senders;

namespace ServiceComponents.AspNet
{
    public class EventRecorderService
    {
        public async Task<T> ExecuteAndWaitFor<T>(Action action, Func<T, ICorrelation, bool> filter, CancellationToken cancellationToken) where T : IEvent
        {
            return default;
        }

        internal async Task PreHandleEvent(IEvent evnt, ICorrelation correlation)
        {

        }
    }

    public class EventRecorderFeeder : IPreHandleEvent
    {
        private readonly EventRecorderService _eventRecorder;
        private readonly ICorrelation _correlation;

        public EventRecorderFeeder(EventRecorderService eventRecorder, ICorrelation correlation)
        {
            _eventRecorder = eventRecorder;
            _correlation = correlation;
        }

        public async Task PreHandleAsync(IEvent @event, CancellationToken cancellationToken = default)
        {
            await _eventRecorder.PreHandleEvent(@event, _correlation);
        }
    }

    public class TestCommand : Command
    {
        public TestCommand() : base(default) { }
    }

    public class TestEvent : Event
    {
        public TestEvent() : base(default) { }
    }

    public class xxx
    {
        private readonly EventRecorderService _recorder;

        public xxx(EventRecorderService recorder)
        {
            _recorder = recorder;
        }

        public async Task Test()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var awaiterTask = await _recorder.ExecuteAndWaitFor<TestEvent>(() => { }, (evnt, correlation) => false, timeout.Token);

        }
    }
}
