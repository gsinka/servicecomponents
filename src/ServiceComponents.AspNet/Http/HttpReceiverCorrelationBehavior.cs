using System;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using ServiceComponents.Infrastructure.CorrelationContext;

namespace ServiceComponents.AspNet.Http
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

            _correlation.CorrelationId =  string.IsNullOrWhiteSpace(correlationId) ? Guid.NewGuid().ToString() : correlationId;
            _correlation.CausationId = _httpContextAccessor.HttpContext.Request.Headers[_httpRequestOptions.CausationIdHeaderKey];
            _correlation.CurrentId = currentId;

            // ForContext destructures immediately, even when Debug is disabled
            if (_log.IsEnabled(LogEventLevel.Debug)) {
                _log.ForContext("correlation", _correlation, true).Debug("Correlation updated from HttpContext");
            }
        }
    }
}