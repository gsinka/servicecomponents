using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.AspNet.Http.Senders
{
    public interface ISendHttpQuery
    {
        Task<TResult> SendAsync<TResult>(IQuery<TResult> query, IDictionary<string, string> headers, CancellationToken cancellationToken = default);
    }
}