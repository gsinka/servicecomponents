using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Monitoring;

namespace ServiceComponents.Infrastructure.Monitoring
{
    [CounterMetric("generic_request_counter", "Request counter")]
    public class RequestCounterMetric : RequestMetrics
    {
        public RequestCounterMetric(ICommand command) : base(command)
        { }

        public RequestCounterMetric(IQuery query) : base(query)
        { }

        public RequestCounterMetric(IEvent evnt) : base(evnt)
        { }
    }

    [GaugeMetric("generic_request_gauge", "Actual request counter")]
    public class RequestGaugeMetric : GaugeMetric
    {
        public RequestGaugeMetric(string name, string title) : base(name, title)
        {
        }
    }


}