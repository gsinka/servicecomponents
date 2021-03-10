using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Autofac;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using Serilog;
using ServiceComponents.Infrastructure.Rabbit;

namespace ReferenceApplication.AspNet.Wireup
{
    public class RabbitSetup : IStartable
    {
        private readonly ILogger _log;
        private readonly ILifetimeScope _scope;
        private readonly IModel _channel;
        private readonly IConfiguration _configuration;

        public RabbitSetup(ILogger log, ILifetimeScope scope, IModel channel, IConfiguration configuration)
        {
            _log = log;
            _scope = scope;
            _channel = channel;
            _configuration = configuration;
        }

        public void Start()
        {
            var ttls = _configuration.GetValue("rabbit:retryIntervals", "1000, 3000, 10000")
                .Split(new[] {',', ';'}, StringSplitOptions.RemoveEmptyEntries)
                .Select(int.Parse)
                .OrderBy(i => i)
                .ToArray();

            _channel.ExchangeDeclare("test", "direct", false, true);
            _channel.AddRabbitRetry(_scope, "test", ttls);
            _channel.QueueBind("test", "test", string.Empty);
            
            for (var i = 0; i < Environment.ProcessorCount; i++) {
                _log.Verbose("Starting consumer consumer-{consumerId}", i);
                _scope.ResolveKeyed<RabbitConsumer>($"consumer-{i}").StartAsync(CancellationToken.None).Wait();
            }
        }
    }
}