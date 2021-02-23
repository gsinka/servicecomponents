﻿using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using Serilog.Context;
using ServiceComponents.Infrastructure.Http;

namespace ServiceComponents.Infrastructure.Rabbit
{
    public class RabbitConsumer
    {
        private readonly ILogger _log;
        private readonly ILifetimeScope _rootScope;
        private readonly IModel _model;
        private readonly string _queue;
        private readonly EventingBasicConsumer _consumer;

        public RabbitConsumer(ILogger log, ILifetimeScope rootScope, IModel model, string queue)
        {
            _log = log;
            _rootScope = rootScope;
            _model = model;
            _queue = queue;
            _consumer = new EventingBasicConsumer(_model);
            _consumer.Received += ConsumerOnReceived;
        }

        private void ConsumerOnReceived(object sender, BasicDeliverEventArgs e)
        {
            var consumer = sender as EventingBasicConsumer;
            
            _log.Verbose("Receiving message from RabbitMQ queue '{queue}'", _queue);

            using var scope = _rootScope.BeginLifetimeScope();

            var payload = Encoding.UTF8.GetString(e.Body.ToArray());
            
            object queryResult = null;
            
            new RequestParser().ParseAsync(
                payload, 
                e.BasicProperties.Type,
                async (command, token) => { await scope.Resolve<IReceiveRabbitCommand>().ReceiveAsync(command, e, CancellationToken.None);},
                async (query, token) => { queryResult = await scope.Resolve<IReceiveRabbitQuery>().ReceiveAsync((dynamic)query, e, CancellationToken.None);}, 
                async (@event, token) => { await scope.Resolve<IReceiveRabbitEvent>().ReceiveAsync(@event, e, CancellationToken.None);}, 
                CancellationToken.None).Wait();

            _model.BasicAck(e.DeliveryTag, false);

            if (queryResult != null) {
                if (string.IsNullOrEmpty(e.BasicProperties.ReplyTo)) {
                    _log.Error("Received query on RabbitMQ but no reply_to address defined for sending query result");
                } else {
                    var replyProps = _model.CreateBasicProperties();
                    replyProps.CorrelationId = e.BasicProperties.CorrelationId;
                    _model.BasicPublish("", e.BasicProperties.ReplyTo, false, replyProps, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(queryResult, Formatting.None)));
                }
            }
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _model.BasicConsume(_queue, false, _consumer);
        }
    }
}