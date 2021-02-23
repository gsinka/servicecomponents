using System.Threading;
using System.Threading.Tasks;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Infrastructure.Dispatchers;

namespace ServiceComponents.Infrastructure.Receivers
{
    public class CommandReceiver : IReceiveCommand
    {
        private readonly ILogger _log;
        private readonly IDispatchCommand _commandDispatcher;

        public CommandReceiver(ILogger log, IDispatchCommand commandDispatcher)
        {
            _log = log;
            _commandDispatcher = commandDispatcher;
        }

        public async Task ReceiveAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand
        {
            await _commandDispatcher.DispatchAsync(command, cancellationToken);
        }
    }
}