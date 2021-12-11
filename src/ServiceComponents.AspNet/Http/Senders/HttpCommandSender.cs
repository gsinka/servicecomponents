using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.AspNet.Exceptions;

namespace ServiceComponents.AspNet.Http.Senders
{
    public class HttpCommandSender : HttpSender, ISendHttpCommand
    {
        private readonly ILogger _log;

        public HttpCommandSender(ILogger log, HttpClient httpClient, Uri requestUri, IOptions<HttpRequestOptions> options, IExceptionMapperService exceptionMapperService) 
            : base(log, httpClient, requestUri, options, exceptionMapperService)
        {
            _log = log;
        }

        public async Task SendAsync<T>(T command, IDictionary<string, string> headers, CancellationToken cancellationToken = default) where T : ICommand
        {
            await SendRequest(command, headers, cancellationToken);
        }
    }
}