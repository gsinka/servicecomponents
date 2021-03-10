using System.Threading;
using System.Threading.Tasks;
using ReferenceApplication.Api;
using Serilog;
using ServiceComponents.Application.Mediator;

namespace ReferenceApplication.Application
{
    public class LongCommandHandler : IHandleCommand<LongCommand>
    {
        private readonly ILogger _log;

        public LongCommandHandler(ILogger log)
        {
            _log = log;
        }

        public async Task HandleAsync(LongCommand command, CancellationToken cancellationToken = default)
        {
            _log.Information("Handling long running command");
            await Task.Delay(10000, cancellationToken);
            _log.Information("Long running command handled");
        }
    }
}