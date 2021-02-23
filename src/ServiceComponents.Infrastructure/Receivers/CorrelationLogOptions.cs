namespace ServiceComponents.Infrastructure.Receivers
{
    public class CorrelationLogOptions
    {
        public string CorrelationIdPropertyName { get; set; } = "correlation-id";
        public string CausationIdPropertyName { get; set; } = "causation-id";
        public string CommandIdPropertyName { get; set; } = "command-id";
        public string QueryIdPropertyName { get; set; } = "query-id";
        public string EventIdPropertyName { get; set; } = "event-id";
    }
}