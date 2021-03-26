using ServiceComponents.Application.Monitoring;

namespace ServiceComponents.Infrastructure.Monitoring
{
    [GaugeMetric("generic_request_gauge", "Actual request counter")]
    public class RequestGaugeMetric : GaugeMetric
    {
        public RequestGaugeMetric(string name, string title) : base(name, title)
        {
        }
    }
}