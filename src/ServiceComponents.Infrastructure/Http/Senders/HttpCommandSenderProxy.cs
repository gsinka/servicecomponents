using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Senders;

namespace ServiceComponents.Infrastructure.Http.Senders
{
    public class HttpCommandSenderProxy : ISendCommand
    {
        private readonly ISendHttpCommand _next;

        public HttpCommandSenderProxy(ISendHttpCommand next)
        {
            _next = next;
        }

        public async Task SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand
        {
            await _next.SendAsync(command, new Dictionary<string, string>(), cancellationToken);
        }
    }
}