using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Serilog;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.AspNet.Http
{
    public class HttpEventReceiverCorrelationBehavior : HttpReceiverCorrelationBehavior, IReceiveHttpEvent
    {
        private readonly IReceiveHttpEvent _next;

        public HttpEventReceiverCorrelationBehavior(ILogger log, IHttpContextAccessor httpContextAccessor, IOptions<HttpRequestOptions> httpRequestOptions, Infrastructure.CorrelationContext.Correlation correlation, IReceiveHttpEvent next) 
            : base(log, httpContextAccessor, httpRequestOptions, correlation)
        {
            _next = next;
        }

        public async Task ReceiveAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IEvent
        {
            UpdateCorrelation(@event.EventId);
            await _next.ReceiveAsync(@event, cancellationToken);
        }
    }
}