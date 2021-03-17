using System.Threading;
using System.Threading.Tasks;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Core.Extensions;
using ServiceComponents.Infrastructure.Receivers;

namespace ServiceComponents.AspNet.Http
{
    public class HttpCommandReceiver : IReceiveHttpCommand
    {
        private readonly ILogger _log;
        private readonly IReceiveCommand _commandReceiver;
        
        public HttpCommandReceiver(ILogger log, IReceiveCommand commandReceiver)
        {
            _log = log;
            _commandReceiver = commandReceiver;
        }
        public async Task ReceiveAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand
        {
            _log.Verbose("Receiving {commandType} on http", command.DisplayName());
            await _commandReceiver.ReceiveAsync(command, cancellationToken);
        }
    }
}
