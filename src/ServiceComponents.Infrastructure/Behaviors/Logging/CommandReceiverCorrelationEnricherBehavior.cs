using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Infrastructure.CorrelationContext;
using ServiceComponents.Infrastructure.Receivers;

namespace ServiceComponents.Infrastructure.Behaviors.Logging
{
    public class CommandReceiverCorrelationEnricherBehavior : ReceiverCorrelationBehavior, IReceiveCommand
    {
        private readonly IReceiveCommand _next;

        public CommandReceiverCorrelationEnricherBehavior(IOptions<CorrelationLogOptions> options, Correlation correlation, IReceiveCommand next)
            : base(options, correlation)
        {
            _next = next;
        }

        public async Task ReceiveAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand
        {
            Enrich(Options.CommandIdPropertyName, command.CommandId);
            await _next.ReceiveAsync(command, cancellationToken);
        }
    }
}