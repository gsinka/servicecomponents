using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Infrastructure.Receivers;

namespace ServiceComponents.Infrastructure.Rabbit
{
    public class RabbitQueryReceiver : IReceiveRabbitQuery
    {
        private readonly IReceiveQuery _queryReceiver;

        public RabbitQueryReceiver(IReceiveQuery queryReceiver)
        {
            _queryReceiver = queryReceiver;
        }

        public async Task<T> ReceiveAsync<T>(IQuery<T> query, BasicDeliverEventArgs args, CancellationToken cancellationToken)
        {
            return await _queryReceiver.ReceiveAsync(query, cancellationToken);
        }
    }
}