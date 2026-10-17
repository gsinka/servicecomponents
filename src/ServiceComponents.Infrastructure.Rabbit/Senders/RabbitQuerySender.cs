using System;
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
        private readonly IChannel _channel;
        private readonly string _exchange;
        private readonly string _routingKey;

        public RabbitQuerySender(ILogger log, IChannel channel, string exchange, string routingKey)
        {
            _log = log;
            _channel = channel;
            _exchange = exchange;
            _routingKey = routingKey;
        }

        public async Task<TResult> SendAsync<TResult>(IQuery<TResult> query, BasicProperties basicProperties, CancellationToken cancellationToken = default)
        {
            var commandJson = JsonConvert.SerializeObject(query, Formatting.None);
            basicProperties.Type = query.AssemblyVersionlessQualifiedName();

            if (string.IsNullOrEmpty(basicProperties.CorrelationId)) {
                basicProperties.CorrelationId = Guid.NewGuid().ToString();
            }

            // The first matching reply completes this; awaiting it keeps the calling thread free while the
            // remote service works (a blocking wait here starves the thread pool under concurrent queries).
            var responseSource = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            var replyConsumer = new AsyncEventingBasicConsumer(_channel);
            var replyQueueDeclareOk = await _channel.QueueDeclareAsync(cancellationToken: cancellationToken);
            var replyQueueName = replyQueueDeclareOk.QueueName;
            basicProperties.ReplyTo = replyQueueName;

            replyConsumer.ReceivedAsync += (sender, args) => {
                if (args.BasicProperties.CorrelationId == basicProperties.CorrelationId) {
                    responseSource.TrySetResult(Encoding.UTF8.GetString(args.Body.Span));
                }
                return Task.CompletedTask;
            };

            await _channel.BasicConsumeAsync(replyQueueName, autoAck: true, consumer: replyConsumer, cancellationToken: cancellationToken);

            _log.ForContext("query", query, true).Verbose("Sending {queryType} using RabbitMQ sender to exchange '{exchange}', routing-key: '{routingKey}'", query.DisplayName(), _exchange, _routingKey);

            await _channel.BasicPublishAsync("", _exchange, false, basicProperties, Encoding.UTF8.GetBytes(commandJson), cancellationToken);

            var response = await WaitForResponse(responseSource.Task, cancellationToken);

            await _channel.QueueDeleteAsync(replyQueueName, false, false, cancellationToken: cancellationToken);

            return JsonConvert.DeserializeObject<TResult>(response);
        }

        /// <summary>
        /// Waits like BlockingCollection.Take(cancellationToken): an already canceled token wins even over a
        /// reply that has arrived, and cancellation surfaces as OperationCanceledException with the caller's token.
        /// </summary>
        private static async Task<string> WaitForResponse(Task<string> response, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try {
                return await response.WaitAsync(cancellationToken);
            }
            catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested) {
                throw new OperationCanceledException(cancellationToken);
            }
        }
    }
}
