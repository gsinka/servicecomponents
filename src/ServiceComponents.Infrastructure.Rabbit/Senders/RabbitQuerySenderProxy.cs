using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Senders;

namespace ServiceComponents.Infrastructure.Rabbit.Senders
{
    public class RabbitQuerySenderProxy : ISendQuery
    {
        private readonly IModel _model;
        private readonly ISendRabbitQuery _rabbitSender;

        public RabbitQuerySenderProxy(IModel model, ISendRabbitQuery rabbitSender)
        {
            _model = model;
            _rabbitSender = rabbitSender;
        }

        public async Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
        {
            var basicProperties = _model.CreateBasicProperties();
            basicProperties.Headers = new Dictionary<string, object>();

            return await _rabbitSender.SendAsync(query, basicProperties, cancellationToken);

        }
    }
}