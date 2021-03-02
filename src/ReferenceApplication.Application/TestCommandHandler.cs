using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using NHibernate;
using ReferenceApplication.Api;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application;
using ServiceComponents.Application.Senders;
using ServiceComponents.Core;
using ServiceComponents.Core.ExtensionMethods;
using ServiceComponents.Infrastructure.NHibernate;

namespace ReferenceApplication.Application
{
    public class TestCommandHandler : TransactionCommandHandler<TestCommand>
    {
        private readonly ISession _session;

        public TestCommandHandler(ILogger log, ICorrelation correlation, ISendQuery querySender, IPublishEvent eventPublisher, ISession session, IClock clock) 
            : base(log, correlation, querySender, eventPublisher, session, clock)
        {
            _session = session;
        }

        override protected async Task HandleAsync(TestCommand command, ITransaction transaction, CancellationToken cancellationToken = default)
        {
            var queryResult = await SendAsync(new TestQuery("from command handler"), cancellationToken);
            Log.Information("Query called from command handler with result: {queryResult}", queryResult);

            Log.Information("Connection string: {connection}", _session.Connection.ConnectionString);

            Log.Information("{command} handled", command.DisplayName());

            Log.Information("Publishing event");
            await PublishAsync(new TestEvent("event from command handler"), cancellationToken);
        }
    }
}
