using ServiceComponents.Application.Monitoring;

namespace ReferenceApplication.Application.TestProcess
{
    [SummaryMetric("business_process", "Business processes")]
    public class MultiStepBusinessProcessMetric
    {
        [MetricField("process_name", "Process")]
        public string ProcessName { get; }
        
        [MetricField("step_name", "Process step" )]
        public string StepName { get; }

        public MultiStepBusinessProcessMetric(string processName, string stepName)
        {
            ProcessName = processName;
            StepName = stepName;
        }
    }
}