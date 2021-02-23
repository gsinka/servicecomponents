using System;
using System.Text;
using RabbitMQ.Client.Events;
using Serilog;
using ServiceComponents.Infrastructure.CorrelationContext;

namespace ServiceComponents.Infrastructure.Rabbit
{
    public abstract class RabbitReceiverCorrelationBehavior
    {
        private readonly ILogger _log;
        private readonly Correlation _correlation;

        protected RabbitReceiverCorrelationBehavior(ILogger log, Correlation correlation)
        {
            _log = log;
            _correlation = correlation;
        }

        protected void UpdateCorrelation(BasicDeliverEventArgs args, string currentId)
        {
            _correlation.CorrelationId = args.BasicProperties.IsCorrelationIdPresent() ? args.BasicProperties.CorrelationId : Guid.NewGuid().ToString();
            if (args.BasicProperties.Headers != null && args.BasicProperties.Headers.ContainsKey("causation_id")) _correlation.CausationId = Encoding.UTF8.GetString((byte[])args.BasicProperties.Headers["causation_id"]);
            _correlation.CurrentId = currentId;

            _log.ForContext("correlation", _correlation, true).Debug("Correlation updated from RabbitMQ basic properties");
        }
    }
}