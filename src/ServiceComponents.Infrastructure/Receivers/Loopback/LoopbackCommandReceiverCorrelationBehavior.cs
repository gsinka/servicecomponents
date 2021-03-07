using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Infrastructure.CorrelationContext;

namespace ServiceComponents.Infrastructure.Receivers
{
    public class LoopbackCommandReceiverCorrelationBehavior : LoopbackReceiverCorrelationBehavior, IReceiveLoopbackCommand
    {
        private readonly IReceiveLoopbackCommand _next;
        private readonly Correlation _correlation;

        public LoopbackCommandReceiverCorrelationBehavior(IReceiveLoopbackCommand next, Correlation correlation) : base(correlation)
        {
            _next = next;
            _correlation = correlation;
        }

        public async Task ReceiveAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand
        {
            SetCorrelation(command.CommandId);
            await _next.ReceiveAsync(command, cancellationToken);
            ResetCorrelation();
        }
    }
}