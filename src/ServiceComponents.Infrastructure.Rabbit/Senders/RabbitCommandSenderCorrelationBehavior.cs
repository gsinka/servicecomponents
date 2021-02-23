using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application;

namespace ServiceComponents.Infrastructure.Rabbit.Senders
{
    public class RabbitCommandSenderCorrelationBehavior : RabbitSenderCorrelationBehavior, ISendRabbitCommand
    {
        private readonly ISendRabbitCommand _next;

        public RabbitCommandSenderCorrelationBehavior(ILogger log, IModel model, ICorrelation correlation, ISendRabbitCommand next) : base(log, correlation)
        {
            _next = next;
        }

        public async Task SendAsync<T>(T command, IBasicProperties basicProperties, CancellationToken cancellationToken) where T : ICommand
        {
            UpdateCorrelation(basicProperties);
            await _next.SendAsync(command, basicProperties, cancellationToken);
        }
    }
}