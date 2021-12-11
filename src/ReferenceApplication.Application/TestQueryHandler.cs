using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NHibernate;
using NHibernate.Linq;
using ReferenceApplication.Api;
using ReferenceApplication.Application.Entities;
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
        private readonly ISession _session;

        public TestQueryHandler(ILogger log, ICorrelation correlation, ISendQuery querySender, ISendCommand commandSender/*, ISession session*/) : base(log, correlation, querySender)
        {
            _commandSender = commandSender;
            //_session = session;
        }

        override public async Task<string> HandleAsync(TestQuery query, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("query failed");
            Log.Information("{query} handled", query.DisplayName());

            //await _commandSender.SendAsync(new ErrorCommand(666, "Request from Test"), cancellationToken);
            //return "";
            
            //var entities = await _session.Query<TestEntity>().ToListAsync(cancellationToken);
            //return string.Join(',', entities.Select(entity => entity.Name));

            return $"{query.Data} result";
        }
    }
}
