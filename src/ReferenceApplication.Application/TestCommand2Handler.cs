using System;
using System.Threading;
using System.Threading.Tasks;
using ReferenceApplication.Api;
using Serilog;
using ServiceComponents.Application;
using ServiceComponents.Application.Mediator;
using ServiceComponents.Application.Senders;

namespace ReferenceApplication.Application
{
    public class TestCommand2Handler : CommandHandler<TestCommand2>
    {
        public TestCommand2Handler(ILogger log, ICorrelation correlation, ISendQuery querySender, IPublishEvent eventPublisher) : base(log, correlation, querySender, eventPublisher)
        {
        }

        override public async Task HandleAsync(TestCommand2 command, CancellationToken cancellationToken = default)
        {
            Log.Information("Test command 2 handled");
        }
    }
}
