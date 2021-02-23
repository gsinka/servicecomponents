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
    public class TestQueryHandler : QueryHandler<TestQuery, string>
    {
        public TestQueryHandler(ILogger log, ICorrelation correlation, ISendQuery querySender) : base(log, correlation, querySender)
        { }

        override public async Task<string> HandleAsync(TestQuery query, CancellationToken cancellationToken = default)
        {
            Log.Information("{query} handled", query.DisplayName());
            return $"{query.Data} handled";
        }

    }
}
