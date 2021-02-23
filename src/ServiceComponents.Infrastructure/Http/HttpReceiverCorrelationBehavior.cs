using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Serilog;
using ServiceComponents.Infrastructure.CorrelationContext;

namespace ServiceComponents.Infrastructure.Http
{
    public abstract class HttpReceiverCorrelationBehavior
    {
        private readonly ILogger _log;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HttpRequestOptions _httpRequestOptions;
        private readonly Correlation _correlation;

        protected HttpReceiverCorrelationBehavior(ILogger log, IHttpContextAccessor httpContextAccessor, IOptions<HttpRequestOptions> httpRequestOptions, Correlation correlation)
        {
            _log = log;
            _httpContextAccessor = httpContextAccessor;
            _httpRequestOptions = httpRequestOptions.Value;
            _correlation = correlation;
        }

        protected void UpdateCorrelation(string currentId)
        {
            var correlationId = _httpContextAccessor.HttpContext.Request.Headers[_httpRequestOptions.CorrelationIdHeaderKey];

            _correlation.CorrelationId =  string.IsNullOrWhiteSpace(correlationId) ? Guid.NewGuid().ToString() : (string) correlationId;
            _correlation.CausationId = _httpContextAccessor.HttpContext.Request.Headers[_httpRequestOptions.CausationIdHeaderKey];
            _correlation.CurrentId = currentId;

            _log.ForContext("correlation", _correlation, true).Debug("Correlation updated from HttpContext");
        }
    }
}