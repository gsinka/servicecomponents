using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using ServiceComponents.Infrastructure.Receivers;


namespace ServiceComponents.Infrastructure.Rabbit
{
    public interface IRabbitConsumer
    {
        public string ConsumerTag { get; }
        public bool IsLive { get; }
    }

    public class RabbitConsumer : IRabbitConsumer
    {
        private readonly ILogger _log;
        private readonly ILifetimeScope _rootScope;
        private readonly IChannel _channel;
        private readonly string _queue;
        private readonly AsyncEventingBasicConsumer _consumer;

        public string ConsumerTag { get; private set; }
        public bool IsLive => _consumer.IsRunning;

        public RabbitConsumer(ILogger log, ILifetimeScope rootScope, IChannel channel, string queue, string consumerTag = default)
        {
            _log = log;
            _rootScope = rootScope;
            _channel = channel;
            _queue = queue;
            ConsumerTag = consumerTag;
            _consumer = new AsyncEventingBasicConsumer(_channel);
            _consumer.ReceivedAsync += ConsumerOnReceivedAsync;
        }

        private async Task ConsumerOnReceivedAsync(object sender, BasicDeliverEventArgs e)
        {
            _log.Verbose("Receiving message from RabbitMQ queue '{queue}'", _queue);

            using var scope = _rootScope.BeginLifetimeScope();

            try {
                var payload = Encoding.UTF8.GetString(e.Body.ToArray());

                object queryResult = null;

                await new RequestParser().ParseAsync(
                    payload,
                    e.BasicProperties.Type,
                    async (command, token) => { await scope.Resolve<IReceiveRabbitCommand>().ReceiveAsync(command, e, CancellationToken.None); },
                    async (query, token) => { queryResult = await scope.Resolve<IReceiveRabbitQuery>().ReceiveAsync((dynamic)query, e, CancellationToken.None); },
                    async (@event, token) => { await scope.Resolve<IReceiveRabbitEvent>().ReceiveAsync(@event, e, CancellationToken.None); },
                    async (payload, token) => { await scope.Resolve<IReceiveRabbitMessage>().ReceiveAsync(payload, e, CancellationToken.None); },
                    CancellationToken.None);

                await _channel.BasicAckAsync(e.DeliveryTag, false);

                if (queryResult != null) {
                    if (string.IsNullOrEmpty(e.BasicProperties.ReplyTo)) {
                        _log.Error("Received query on RabbitMQ but no reply_to address defined for sending query result");
                    }
                    else {
                        var replyProps = new BasicProperties
                        {
                            CorrelationId = e.BasicProperties.CorrelationId
                        };
                        await _channel.BasicPublishAsync("", e.BasicProperties.ReplyTo, false, replyProps, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(queryResult, Formatting.None)));
                    }
                }
            }
            catch (Exception exception) {
                _log.Error(exception, "Exception during receiving message from RabbitMQ queue '{queue}'", _queue);
                await _channel.BasicRejectAsync(e.DeliveryTag, false);
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (ConsumerTag == default) {
                ConsumerTag = await _channel.BasicConsumeAsync(_queue, false, _consumer, cancellationToken);
            }
            else {
                await _channel.BasicConsumeAsync(_queue, false, ConsumerTag, _consumer, cancellationToken);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            await _channel.BasicCancelAsync(ConsumerTag, cancellationToken: cancellationToken);
        }
    }
}
