using ServiceComponents.Application.Monitoring;

namespace ReferenceApplication.Application.TestProcess
{
    [MetricDescription("business_process", "Business processes")]
    public class BusinessProcessMetric
    {
        public string ProcessName { get; }

        public BusinessProcessMetric(string processName)
        {
            ProcessName = processName;
        }
    }
}