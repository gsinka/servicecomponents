using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Infrastructure.CorrelationContext;

namespace ServiceComponents.Infrastructure.Rabbit
{
    public class RabbitQueryReceiverCorrelationBehavior : RabbitReceiverCorrelationBehavior, IReceiveRabbitQuery
    {
        private readonly IReceiveRabbitQuery _next;

        public RabbitQueryReceiverCorrelationBehavior(ILogger log, Correlation correlation, IReceiveRabbitQuery next) : base(log, correlation)
        {
            _next = next;
        }

        public async Task<T> ReceiveAsync<T>(IQuery<T> query, BasicDeliverEventArgs args, CancellationToken cancellationToken)
        {
            UpdateCorrelation(args, query.QueryId);
            return await _next.ReceiveAsync(query, args, cancellationToken);
        }
    }
}