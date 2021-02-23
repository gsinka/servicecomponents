using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application;

namespace ServiceComponents.Infrastructure.Http.Senders
{
    public class HttpCommandSenderCorrelationBehavior : HttpSenderCorrelationBehavior, ISendHttpCommand
    {
        private readonly ISendHttpCommand _next;
        
        public HttpCommandSenderCorrelationBehavior(ILogger log, ICorrelation correlation, IOptions<HttpRequestOptions> options, ISendHttpCommand next) : base(log, correlation, options)
        {
            _next = next;
        }

        public async Task SendAsync<T>(T command, IDictionary<string, string> headers, CancellationToken cancellationToken = default) where T : ICommand
        {
            SetCorrelation(headers);
            await _next.SendAsync(command, headers, cancellationToken);
        }
    }
}