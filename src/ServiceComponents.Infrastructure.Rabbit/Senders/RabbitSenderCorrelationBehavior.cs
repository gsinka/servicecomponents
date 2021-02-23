using System;
using Autofac;
using RabbitMQ.Client;
using Serilog;
using ServiceComponents.Application;

namespace ServiceComponents.Infrastructure.Rabbit.Senders
{
    public abstract class RabbitSenderCorrelationBehavior
    {
        private readonly ILogger _log;
        protected readonly ICorrelation Correlation;

        protected RabbitSenderCorrelationBehavior(ILogger log, ICorrelation correlation)
        {
            _log = log;
            Correlation = correlation;
        }

        protected void UpdateCorrelation(IBasicProperties basicProperties)
        {
            basicProperties.CorrelationId = string.IsNullOrWhiteSpace(Correlation.CorrelationId) ? Guid.NewGuid().ToString() : Correlation.CorrelationId;
            if (!string.IsNullOrWhiteSpace(Correlation.CurrentId)) basicProperties.Headers.Add("causation_id", Correlation.CurrentId);

            _log.ForContext("correlation", new { correlationId = basicProperties.CorrelationId, causationId = Correlation.CurrentId }, true).Debug("RabbitMQ basic properties updated with correlation");
        }
    }
}