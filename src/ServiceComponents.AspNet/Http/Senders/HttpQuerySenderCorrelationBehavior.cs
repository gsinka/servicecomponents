using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application;

namespace ServiceComponents.AspNet.Http.Senders
{
    public class HttpQuerySenderCorrelationBehavior : HttpSenderCorrelationBehavior, ISendHttpQuery
    {
        private readonly ISendHttpQuery _next;

        public HttpQuerySenderCorrelationBehavior(ILogger log, ICorrelation correlation, IOptions<HttpRequestOptions> options, ISendHttpQuery next) : base(log, correlation, options)
        {
            _next = next;
        }

        public async Task<TResult> SendAsync<TResult>(IQuery<TResult> query, IDictionary<string, string> headers, CancellationToken cancellationToken = default)
        {
            SetCorrelation(headers);
            return await _next.SendAsync(query, headers, cancellationToken);
        }
    }
}