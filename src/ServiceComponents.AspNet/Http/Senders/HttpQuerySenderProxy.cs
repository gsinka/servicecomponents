using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Senders;

namespace ServiceComponents.AspNet.Http.Senders
{
    public class HttpQuerySenderProxy : ISendQuery
    {
        private readonly ISendHttpQuery _next;

        public HttpQuerySenderProxy(ISendHttpQuery next)
        {
            _next = next;
        }

        public async Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
        {
            return await _next.SendAsync(query, new Dictionary<string, string>(), cancellationToken);
        }
    }
}