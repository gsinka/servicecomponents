using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.AspNet.Http.Senders
{
    public interface ISendHttpCommand
    {
        Task SendAsync<T>(T command, IDictionary<string, string> headers, CancellationToken cancellationToken = default) where T : ICommand;
    }
}