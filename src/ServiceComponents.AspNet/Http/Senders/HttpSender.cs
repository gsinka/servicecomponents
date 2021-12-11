using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Serilog;
using ServiceComponents.AspNet.Exceptions;
using ServiceComponents.Core.Extensions;

namespace ServiceComponents.AspNet.Http.Senders
{
    public abstract class HttpSender
    {
        private readonly ILogger _log;
        private readonly HttpClient _httpClient;
        protected readonly Uri RequestUri;
        private readonly IExceptionMapperService _exceptionMapperService;
        private readonly HttpRequestOptions _options;

        protected HttpSender(ILogger log, HttpClient httpClient, Uri requestUri, IOptions<HttpRequestOptions> options, IExceptionMapperService exceptionMapperService)
        {
            _log = log;
            _httpClient = httpClient;
            RequestUri = requestUri;
            _exceptionMapperService = exceptionMapperService;
            _options = options.Value;
        }

        protected async Task<HttpResponseMessage> SendRequest(object request, IDictionary<string, string> header, CancellationToken cancellationToken)
        {
            _log.Verbose("Sending request {requestType} to {requestUri}", request.GetType().Name, RequestUri.ToString());

            var objectJson = JsonConvert.SerializeObject(request, Formatting.None);
            var content = new StringContent(objectJson);
            
            content.Headers.Add(_options.DomainTypeHeaderKey, request.AssemblyVersionlessQualifiedName());
            

            foreach (var (key, value) in header)
            {
                content.Headers.Add(key, value);
            }

            _httpClient.DefaultRequestHeaders.Add("accept", "application/json");

            var result = await _httpClient.PostAsync(RequestUri, content, cancellationToken);
            await _exceptionMapperService.ThrowExceptionIfNeeded(result);
            return result;
        }
    }
}