using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using ReferenceApplication.Api;
using Serilog;
using ServiceComponents.Application;
using ServiceComponents.Application.Mediator;
using ServiceComponents.Application.Senders;

namespace ReferenceApplication.Application
{
    public class TestEventHandler : HandleEvent<TestEvent>
    {
        public TestEventHandler(ILogger log, ICorrelation correlation, ISendCommand commandSender, ISendQuery querySender, IPublishEvent eventPublisher) : base(log, correlation, commandSender, querySender, eventPublisher)
        {
        }

        override async public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken = default)
        {
            //throw new InvalidOperationException();

            await SendAsync(new TestQuery("now what?"), cancellationToken);
            Log.Information("Test event handled. Data: {eventData}", @event.Data);

            await SendAsync(new TestCommand2(), cancellationToken);
        }
    }
    
    public class TestEventHandler2 : HandleEvent<TestEvent>
    {
        public TestEventHandler2(ILogger log, ICorrelation correlation, ISendCommand commandSender, ISendQuery querySender, IPublishEvent eventPublisher) : base(log, correlation, commandSender, querySender, eventPublisher)
        {
        }

        override async public Task HandleAsync(TestEvent @event, CancellationToken cancellationToken = default)
        {
            //throw new InvalidOperationException();
            
            Log.Information("Test event handled with handler 2. Data: {eventData}", @event.Data);
        }
    }
}