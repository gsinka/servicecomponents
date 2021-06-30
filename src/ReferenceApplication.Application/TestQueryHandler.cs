using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
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
        private readonly ISession _session;

        public TestQueryHandler(ILogger log, ICorrelation correlation, ISendQuery querySender, ISession session) : base(log, correlation, querySender)
        {
            _session = session;
        }

        override public async Task<string> HandleAsync(TestQuery query, CancellationToken cancellationToken = default)
        {
            Log.Information("{query} handled", query.DisplayName());

            var entities = await _session.Query<TestEntity>().ToListAsync(cancellationToken);
            return string.Join(',', entities.Select(entity => entity.Name));
        }
    }
}
