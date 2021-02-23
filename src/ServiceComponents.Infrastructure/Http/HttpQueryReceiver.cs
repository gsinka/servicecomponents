using System.Threading;
using System.Threading.Tasks;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Core.ExtensionMethods;
using ServiceComponents.Infrastructure.Receivers;

namespace ServiceComponents.Infrastructure.Http
{
    public class HttpQueryReceiver : IReceiveHttpQuery
    {
        private readonly ILogger _log;
        private readonly IReceiveQuery _queryReceiver;

        public HttpQueryReceiver(ILogger log, IReceiveQuery queryReceiver)
        {
            _log = log;
            _queryReceiver = queryReceiver;
        }
        public async Task<TResult> ReceiveAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
        {
            _log.Verbose("Receiving {queryType} on http", query.DisplayName());
            return await _queryReceiver.ReceiveAsync(query, cancellationToken);
        }
    }
}