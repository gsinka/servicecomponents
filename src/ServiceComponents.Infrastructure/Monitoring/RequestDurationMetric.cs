using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Infrastructure.Monitoring
{
    [MetricDescription("request_duration", "Request duration")]
    public class RequestDurationMetric : RequestMetrics
    {
        public RequestDurationMetric(ICommand command) : base(command)
        { }

        public RequestDurationMetric(IQuery query) : base(query)
        { }

        public RequestDurationMetric(IEvent evnt) : base(evnt)
        { }
    }
}