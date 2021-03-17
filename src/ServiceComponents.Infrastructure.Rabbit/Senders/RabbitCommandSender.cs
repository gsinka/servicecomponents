using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Core.Extensions;

namespace ServiceComponents.Infrastructure.Rabbit.Senders
{
    public class RabbitCommandSender : ISendRabbitCommand
    {
        private readonly ILogger _log;
        private readonly IModel _channel;
        private readonly string _exchange;
        private readonly string _routingKey;

        public RabbitCommandSender(ILogger log, IModel channel, string exchange, string routingKey)
        {
            _log = log;
            _channel = channel;
            _exchange = exchange;
            _routingKey = routingKey;
        }

        public Task SendAsync<T>(T command, IBasicProperties basicProperties, CancellationToken cancellationToken) where T : ICommand
        {
            _log.ForContext("command", command, true).Verbose("Sending {commandType} using RabbitMQ sender to exchange '{exchange}', routing-key: '{routingKey}'", command.DisplayName(), _exchange, _routingKey);

            var commandJson = JsonConvert.SerializeObject(command, Formatting.None);
            basicProperties.Type = command.AssemblyVersionlessQualifiedName();
            _channel.BasicPublish(_exchange, _routingKey, false, basicProperties, Encoding.UTF8.GetBytes(commandJson));
            return Task.CompletedTask;
        }
    }
}
