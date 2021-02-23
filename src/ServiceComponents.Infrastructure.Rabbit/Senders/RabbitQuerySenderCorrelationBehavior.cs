using System.Threading;
using System.Threading.Tasks;
using Autofac;
using RabbitMQ.Client;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application;

namespace ServiceComponents.Infrastructure.Rabbit.Senders
{
    public class RabbitQuerySenderCorrelationBehavior : RabbitSenderCorrelationBehavior, ISendRabbitQuery
    {
        private readonly ISendRabbitQuery _next;

        public RabbitQuerySenderCorrelationBehavior(ILogger log, ILifetimeScope scope, ICorrelation correlation, ISendRabbitQuery next) : base(log, correlation)
        {
            _next = next;
        }

        public async Task<TResult> SendAsync<TResult>(IQuery<TResult> query, IBasicProperties basicProperties, CancellationToken cancellationToken = default)
        {
            UpdateCorrelation(basicProperties);
            return await _next.SendAsync(query, basicProperties, cancellationToken);
        }
    }
}