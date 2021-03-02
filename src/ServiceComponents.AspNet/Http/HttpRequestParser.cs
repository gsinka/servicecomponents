using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Infrastructure.Receivers;

namespace ServiceComponents.AspNet.Http
{
    public class HttpRequestParser
    {
        private readonly HttpRequestOptions _options;

        public HttpRequestParser(IOptions<HttpRequestOptions> options)
        {
            _options = options.Value;
        }

        public async Task Parse(

            HttpRequest httpRequest,
            Func<ICommand, CancellationToken, Task> commandAction,
            Func<IQuery, CancellationToken, Task> queryAction,
            Func<IEvent, CancellationToken, Task> eventAction,
            CancellationToken cancellationToken = default)
        {
            // Get body
            using var reader = new StreamReader(httpRequest.Body, Encoding.UTF8);
            var json = await reader.ReadToEndAsync();
            if (string.IsNullOrEmpty(json)) json = "{}";

            // Get domain type
            var domainType = httpRequest.Headers[_options.DomainTypeHeaderKey].ToString();

            await new RequestParser().ParseAsync(json, domainType, commandAction, queryAction, eventAction, cancellationToken);
        }
    }
}