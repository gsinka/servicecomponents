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
        private readonly IChannel _channel;
        private readonly ISendRabbitCommand _rabbitSender;

        public RabbitCommandSenderProxy(IChannel channel, ISendRabbitCommand rabbitSender)
        {
            _channel = channel;
            _rabbitSender = rabbitSender;
        }

        public async Task SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand
        {
            var basicProperties = new BasicProperties
            {
                Headers = new Dictionary<string, object>()
            };

            await _rabbitSender.SendAsync(command, basicProperties, cancellationToken);
        }
    }
}
