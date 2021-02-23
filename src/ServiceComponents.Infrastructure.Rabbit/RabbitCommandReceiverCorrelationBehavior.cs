using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Infrastructure.CorrelationContext;

namespace ServiceComponents.Infrastructure.Rabbit
{
    public class RabbitCommandReceiverCorrelationBehavior : RabbitReceiverCorrelationBehavior, IReceiveRabbitCommand
    {
        private readonly IReceiveRabbitCommand _next;

        public RabbitCommandReceiverCorrelationBehavior(ILogger log, Correlation correlation, IReceiveRabbitCommand next) : base(log, correlation)
        {
            _next = next;
        }

        public async Task ReceiveAsync<T>(T command, BasicDeliverEventArgs args, CancellationToken cancellationToken) where T : ICommand
        {
            UpdateCorrelation(args, command.CommandId);
            await _next.ReceiveAsync(command, args, cancellationToken);
        }
    }
}