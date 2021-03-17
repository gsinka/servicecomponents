using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Core.Extensions;

namespace ServiceComponents.Infrastructure.Rabbit.Senders
{
    public class RabbitQuerySender : ISendRabbitQuery
    {
        private readonly ILogger _log;
        private readonly IModel _channel;
        private readonly string _exchange;
        private readonly string _routingKey;

        public RabbitQuerySender(ILogger log, IModel channel, string exchange, string routingKey)
        {
            _log = log;
            _channel = channel;
            _exchange = exchange;
            _routingKey = routingKey;
        }

        public Task<TResult> SendAsync<TResult>(IQuery<TResult> query, IBasicProperties basicProperties, CancellationToken cancellationToken = default)
        {
            var commandJson = JsonConvert.SerializeObject(query, Formatting.None);
            basicProperties.Type = query.AssemblyVersionlessQualifiedName();
            
            if (string.IsNullOrEmpty(basicProperties.CorrelationId)) basicProperties.CorrelationId = Guid.NewGuid().ToString();

            var responseQueue = new BlockingCollection<string>();

            var replyConsumer = new EventingBasicConsumer(_channel);
            var replyQueueName = _channel.QueueDeclare().QueueName;
            basicProperties.ReplyTo = replyQueueName;
            
            replyConsumer.Received += (sender, args) => {
                if (args.BasicProperties.CorrelationId == basicProperties.CorrelationId) {
                    responseQueue.Add(Encoding.UTF8.GetString(args.Body.ToArray()));
                    //((EventingBasicConsumer)sender).Model.BasicAck(args.DeliveryTag, false);
                }
            };
            
            _channel.BasicConsume(consumer: replyConsumer, queue: replyQueueName, autoAck: true);

            _log.ForContext("query", query, true).Verbose("Sending {queryType} using RabbitMQ sender to exchange '{exchange}', routing-key: '{routingKey}'", query.DisplayName(), _exchange, _routingKey);

            _channel.BasicPublish("", _exchange, false, basicProperties, Encoding.UTF8.GetBytes(commandJson));

            var response = responseQueue.Take(cancellationToken);

            _channel.QueueDelete(replyQueueName, false, false);

            return Task.FromResult(JsonConvert.DeserializeObject<TResult>(response));
        }
    }
}