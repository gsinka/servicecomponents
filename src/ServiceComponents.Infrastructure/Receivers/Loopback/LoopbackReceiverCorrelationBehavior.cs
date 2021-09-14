using System;
using Serilog;
using ServiceComponents.Application;
using ServiceComponents.Infrastructure.CorrelationContext;

namespace ServiceComponents.Infrastructure.Receivers.Loopback
{
    public abstract class LoopbackReceiverCorrelationBehavior
    {
        private readonly ILogger _log;
        private readonly Correlation _correlation;

        protected LoopbackReceiverCorrelationBehavior(ILogger log, Correlation correlation)
        {
            _log = log;
            _correlation = correlation;
        }

        protected void SetCorrelation(string requestId, ICorrelation originalCorrelation)
        {
            _correlation.CorrelationId = originalCorrelation.CorrelationId ?? Guid.NewGuid().ToString();
            _correlation.CausationId = originalCorrelation.CurrentId;
            _correlation.CurrentId = requestId;

            _log.ForContext("correlation", _correlation,true).Debug("Correlation in loopback scope updated from parent");
        }
    }
}