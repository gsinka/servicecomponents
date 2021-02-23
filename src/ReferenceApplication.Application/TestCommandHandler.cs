using System.Threading;
using System.Threading.Tasks;
using ReferenceApplication.Api;
using Serilog;
using ServiceComponents.Application;
using ServiceComponents.Application.Mediator;
using ServiceComponents.Application.Senders;
using ServiceComponents.Core.ExtensionMethods;

namespace ReferenceApplication.Application
{
    public class TestCommandHandler : CommandHandler<TestCommand>
    {
        public TestCommandHandler(ILogger log, ICorrelation correlation, ISendQuery querySender, IPublishEvent eventPublisher) 
            : base(log, correlation, querySender, eventPublisher)
        { }


        public override async Task HandleAsync(TestCommand command, CancellationToken cancellationToken = default)
        {
            var queryResult = await SendAsync(new TestQuery("from command handler"), cancellationToken);
            Log.Information("Query called from command handler with result: {queryResult}", queryResult);

            //var queryResult2 = await SendAsync(new TestQuery("from command handler"), cancellationToken);
            //Log.Information("Query called from command handler with result: {queryResult}", queryResult2);

            Log.Information("{command} handled", command.DisplayName());
            
            Log.Information("Publishing event");
            await PublishAsync(new TestEvent("event from command handler"), cancellationToken);

        }

    }
}
