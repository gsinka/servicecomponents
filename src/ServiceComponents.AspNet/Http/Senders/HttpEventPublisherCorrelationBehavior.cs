using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application;

namespace ServiceComponents.AspNet.Http.Senders
{
    public class HttpEventPublisherCorrelationBehavior : HttpSenderCorrelationBehavior, IPublishHttpEvent
    {
        private readonly IPublishHttpEvent _next;

        public HttpEventPublisherCorrelationBehavior(ILogger log, ICorrelation correlation, IOptions<HttpRequestOptions> options, IPublishHttpEvent next) : base(log, correlation, options)
        {
            _next = next;
        }

        public async Task PublishAsync<T>(T @event, IDictionary<string, string> headers, CancellationToken cancellationToken = default) where T : IEvent
        {
            SetCorrelation(headers);
            await _next.PublishAsync(@event, headers, cancellationToken);
        }
    }
}