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
    public class HttpEventPublisher : HttpSender, IPublishHttpEvent
    {
        public HttpEventPublisher(ILogger log, HttpClient httpClient, Uri requestUri, IOptions<HttpRequestOptions> options, IExceptionMapperService exceptionMapperService) 
            : base(log, httpClient, requestUri, options, exceptionMapperService)
        {
        }

        public async Task PublishAsync<T>(T @event, IDictionary<string, string> headers, CancellationToken cancellationToken = default) where T : IEvent
        {
            await SendRequest(@event, headers, cancellationToken);
        }
    }
}