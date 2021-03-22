using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using ReferenceApplication.Api;
using Serilog;
using ServiceComponents.Application;
using ServiceComponents.Application.Senders;

namespace ReferenceApplication.Application
{
    public class TestEventHandler : ServiceComponents.Application.Mediator.EventHandler<TestEvent>
    {
        public TestEventHandler(ILogger log, ICorrelation correlation, ISendCommand commandSender, ISendQuery querySender) : base(log, correlation, commandSender, querySender)
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
}