using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Infrastructure.CorrelationContext;

namespace ServiceComponents.AspNet.Http
{
    public class HttpCommandReceiverCorrelationBehavior : HttpReceiverCorrelationBehavior, IReceiveHttpCommand
    {
        private readonly IReceiveHttpCommand _next;

        public HttpCommandReceiverCorrelationBehavior(ILogger log, IHttpContextAccessor httpContextAccessor, IOptions<HttpRequestOptions> httpRequestOptions, Correlation correlation, IReceiveHttpCommand next) 
            : base(log, httpContextAccessor, httpRequestOptions, correlation)
        {
            _next = next;
        }

        public async Task ReceiveAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand
        {
            UpdateCorrelation(command.CommandId);
            await _next.ReceiveAsync(command, cancellationToken);
        }
    }
}