using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Senders;

namespace ServiceComponents.Infrastructure.Rabbit.Senders
{
    public class RabbitCommandSenderProxy : ISendCommand
    {
        private readonly IModel _model;
        private readonly ISendRabbitCommand _rabbitSender;

        public RabbitCommandSenderProxy(IModel model, ISendRabbitCommand rabbitSender)
        {
            _model = model;
            _rabbitSender = rabbitSender;
        }

        public async Task SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand
        {
            var basicProperties = _model.CreateBasicProperties();
            basicProperties.Headers = new Dictionary<string, object>();

            await _rabbitSender.SendAsync(command, basicProperties, cancellationToken);
        }
    }
}