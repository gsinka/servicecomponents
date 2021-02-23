using Microsoft.Extensions.Options;
using Serilog.Context;
using ServiceComponents.Infrastructure.CorrelationContext;
using ServiceComponents.Infrastructure.Receivers;

namespace ServiceComponents.Infrastructure.Behaviors.Logging
{
    public abstract class ReceiverCorrelationBehavior
    {
        protected readonly CorrelationLogOptions Options;
        protected readonly Correlation Correlation;

        protected ReceiverCorrelationBehavior(IOptions<CorrelationLogOptions> options, Correlation correlation)
        {
            Options = options.Value;
            Correlation = correlation;
        }

        protected void Enrich(string currentProperty, string currentId)
        {
            if (!string.IsNullOrEmpty(Correlation.CorrelationId)) LogContext.PushProperty(Options.CorrelationIdPropertyName, Correlation.CorrelationId);
            if (!string.IsNullOrEmpty(Correlation.CausationId)) LogContext.PushProperty(Options.CausationIdPropertyName, Correlation.CausationId);
            
            LogContext.PushProperty(currentProperty, currentId);
        }
    }
}