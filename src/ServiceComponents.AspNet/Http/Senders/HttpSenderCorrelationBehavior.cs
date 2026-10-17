using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using ServiceComponents.Application;

namespace ServiceComponents.AspNet.Http.Senders
{
    public abstract class HttpSenderCorrelationBehavior
    {
        private readonly ILogger _log;
        private readonly ICorrelation _correlation;
        private readonly HttpRequestOptions _options;

        protected HttpSenderCorrelationBehavior(ILogger log, ICorrelation correlation, IOptions<HttpRequestOptions> options)
        {
            _log = log;
            _correlation = correlation;
            _options = options.Value;
        }

        protected void SetCorrelation(IDictionary<string, string> headers)
        {
            if (!string.IsNullOrEmpty(_correlation.CorrelationId)) {
                headers.Add(_options.CorrelationIdHeaderKey, _correlation.CorrelationId);
            }

            headers.Add(_options.CausationIdHeaderKey, _correlation.CurrentId);

            // ForContext destructures immediately, even when Debug is disabled
            if (_log.IsEnabled(LogEventLevel.Debug)) {
                _log.ForContext("correlation", new { correlationId = _correlation.CorrelationId, causationId = _correlation.CausationId }, true).Debug("HttpHeaders updated with correlation");
            }
        }
    }
}