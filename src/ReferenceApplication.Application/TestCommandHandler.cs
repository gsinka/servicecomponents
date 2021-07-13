using System.Threading;
using System.Threading.Tasks;
using NHibernate;
using ReferenceApplication.Api;
using ReferenceApplication.Application.Entities;
using Serilog;
using ServiceComponents.Application;
using ServiceComponents.Application.Mediator;
using ServiceComponents.Application.Senders;
using ServiceComponents.Core.Extensions;

namespace ReferenceApplication.Application
{
    public class TestCommandHandler : CommandHandler<TestCommand>
    {
        public TestCommandHandler(ILogger log, ICorrelation correlation, ISendQuery querySender, IPublishEvent eventPublisher) 
            : base(log, correlation, querySender, eventPublisher)
        {
        }

        override public async Task HandleAsync(TestCommand command, CancellationToken cancellationToken = default)
        {
            //var queryResult = await SendAsync(new TestQuery("from command handler"), cancellationToken);
            //Log.Information("Query called from command handler with result: {queryResult}", queryResult);

            //var queryResult2 = await SendAsync(new TestQuery("from command handler 2"), cancellationToken);

            //using (var tx = _session.BeginTransaction()) {
            //    await _session.SaveAsync(new TestEntity() { Name = "Test".AddRandomPostfix() }, cancellationToken);
            //    await tx.CommitAsync(cancellationToken);
            //}

            Log.Information("{command} handled", command.DisplayName());

            Log.Information("Publishing event");
            await PublishAsync(new TestEvent("event from command handler"), cancellationToken);

        }
    }

    //public class TestCommandHandler : TransactionCommandHandler<TestCommand>
    //{
    //    private readonly ISession _session;

    //    public TestCommandHandler(ILogger log, ICorrelation correlation, ISendQuery querySender, IPublishEvent eventPublisher, ISession session, IClock clock) 
    //        : base(log, correlation, querySender, eventPublisher, session, clock)
    //    {
    //        _session = session;
    //    }

    //    override protected async Task HandleAsync(TestCommand command, ITransaction transaction, CancellationToken cancellationToken = default)
    //    {
    //        var queryResult = await SendAsync(new TestQuery("from command handler"), cancellationToken);
    //        Log.Information("Query called from command handler with result: {queryResult}", queryResult);

    //        Log.Information("Connection string: {connection}", _session.Connection.ConnectionString);

    //        Log.Information("{command} handled", command.DisplayName());

    //        Log.Information("Publishing event");
    //        await PublishAsync(new TestEvent("event from command handler"), cancellationToken);
    //    }
    //}
}
