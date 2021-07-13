using System.Threading;
using System.Threading.Tasks;
using ReferenceApplication.Api;
using Serilog;
using ServiceComponents.Application;
using ServiceComponents.Application.Mediator;
using ServiceComponents.Application.Senders;
using ServiceComponents.Core.Extensions;

namespace ReferenceApplication.Application
{
    public class TestQueryHandler : QueryHandler<TestQuery, string>
    {
        private readonly ISendCommand _commandSender;

        public TestQueryHandler(ILogger log, ICorrelation correlation, ISendQuery querySender, ISendCommand commandSender) : base(log, correlation, querySender)
        {
            _commandSender = commandSender;
        }

        override public async Task<string> HandleAsync(TestQuery query, CancellationToken cancellationToken = default)
        {
            Log.Information("{query} handled", query.DisplayName());

            await _commandSender.SendAsync(new ErrorCommand(666, "Request from Test"), cancellationToken);
            return "";
            //var entities = await _session.Query<TestEntity>().ToListAsync(cancellationToken);
            //return string.Join(',', entities.Select(entity => entity.Name));
        }
    }
}
