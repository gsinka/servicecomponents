using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.AspNet.Http.Senders
{
    public class HttpQuerySender : HttpSender, ISendHttpQuery
    {
        public HttpQuerySender(ILogger log, HttpClient httpClient, Uri requestUri, IOptions<HttpRequestOptions> options) : base(log, httpClient, requestUri, options)
        {
        }

        public async Task<TResult> SendAsync<TResult>(IQuery<TResult> query, IDictionary<string, string> headers, CancellationToken cancellationToken = default)
        {
            var response = await SendRequest(query, headers, cancellationToken);
            var resultJson = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<TResult>(resultJson);
        }
    }
}