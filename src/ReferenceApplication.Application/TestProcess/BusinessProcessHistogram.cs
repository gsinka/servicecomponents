using ServiceComponents.Application.Monitoring;

namespace ReferenceApplication.Application.TestProcess
{
    [LinearHistogramMetric("business_process_linear", "Business process histogram", 100, 100, 10)]
    public class BusinessProcessHistogram
    {
        public string ProcessName { get; }

        public BusinessProcessHistogram(string processName)
        {
            ProcessName = processName;
        }
    }
}