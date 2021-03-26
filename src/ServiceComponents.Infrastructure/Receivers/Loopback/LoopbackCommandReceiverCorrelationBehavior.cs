using System.Threading;
using System.Threading.Tasks;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application;
using ServiceComponents.Infrastructure.CorrelationContext;

namespace ServiceComponents.Infrastructure.Receivers.Loopback
{
    public class LoopbackCommandReceiverCorrelationBehavior : LoopbackReceiverCorrelationBehavior, IReceiveLoopbackCommand
    {
        private readonly IReceiveLoopbackCommand _next;

        public LoopbackCommandReceiverCorrelationBehavior(ILogger log, IReceiveLoopbackCommand next, Correlation correlation) : base(log, correlation)
        {
            _next = next;
        }

        public async Task ReceiveAsync<TCommand>(TCommand command, ICorrelation correlation, CancellationToken cancellationToken = default) where TCommand : ICommand
        {
            SetCorrelation(command.CommandId, correlation);
            await _next.ReceiveAsync(command, correlation, cancellationToken);
        }
    }
}