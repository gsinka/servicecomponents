using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Serilog.Context;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Infrastructure.Receiver
{
    public class CommandReceiverLogBehavior : ReceiverLogBehavior, IReceiveCommand
    {
        private readonly IReceiveCommand _next;

        public CommandReceiverLogBehavior(IOptions<CorrelationLogOptions> logOptions, IReceiveCommand next) : base(logOptions)
        {
            _next = next;
        }

        public async Task ReceiveAsync(ICommand command, string correlationId = null, string causationId = null, string userId = null, string userName = null, CancellationToken cancellationToken = default)
        {
            if (command != null) LogContext.PushProperty(LogOptions.CommandIdPropertyName, command.CommandId);
            PushProperties(correlationId, causationId, userId, userName);
            await _next.ReceiveAsync(command, correlationId, causationId, userId, userName, cancellationToken);
        }
    }
}
