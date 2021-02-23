using Microsoft.Extensions.Options;
using Serilog.Context;

namespace ServiceComponents.Infrastructure.Receiver
{
    public abstract class ReceiverLogBehavior
    {
        protected readonly CorrelationLogOptions LogOptions;

        protected ReceiverLogBehavior(IOptions<CorrelationLogOptions> logOptions)
        {
            LogOptions = logOptions.Value;
        }

        protected void PushProperties(string correlationId, string causationId, string userId, string userName)
        {
            if (!string.IsNullOrEmpty(correlationId)) LogContext.PushProperty(LogOptions.CorrelationIdPropertyName, correlationId);
            if (!string.IsNullOrEmpty(causationId)) LogContext.PushProperty(LogOptions.CausationIdPropertyName, causationId);
            if (!string.IsNullOrEmpty(userId)) LogContext.PushProperty(LogOptions.UserIdPropertyName, userId);
            if (!string.IsNullOrEmpty(userName)) LogContext.PushProperty(LogOptions.UserNamePropertyName, userName);
        }
    }
}