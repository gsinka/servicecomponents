using ServiceComponents.Application;

namespace ServiceComponents.Infrastructure.CorrelationContext
{
    public class Correlation : ICorrelation
    {
        public string CorrelationId { get; set; }
        public string CausationId { get; set; }
        public string CurrentId { get; set; }
    }
}