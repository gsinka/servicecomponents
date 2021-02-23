using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Serilog;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Infrastructure.Http
{
    public class HttpQueryReceiverCorrelationBehavior : HttpReceiverCorrelationBehavior, IReceiveHttpQuery
    {
        private readonly IReceiveHttpQuery _next;

        public HttpQueryReceiverCorrelationBehavior(ILogger log, IHttpContextAccessor httpContextAccessor, IOptions<HttpRequestOptions> httpRequestOptions, CorrelationContext.Correlation correlation, IReceiveHttpQuery next) 
            : base(log, httpContextAccessor, httpRequestOptions, correlation)
        {
            _next = next;
        }

        public async Task<TResult> ReceiveAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
        {
            UpdateCorrelation(query.QueryId);
            return await _next.ReceiveAsync(query, cancellationToken);

        }
    }
}
