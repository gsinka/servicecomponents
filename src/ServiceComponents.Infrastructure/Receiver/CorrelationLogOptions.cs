namespace ServiceComponents.Infrastructure.Receiver
{
    public class CorrelationLogOptions
    {
        public string CorrelationIdPropertyName { get; set; } = "correlationId";
        public string CausationIdPropertyName { get; set; } = "causationId";
        public string UserIdPropertyName { get; set; } = "userId";
        public string UserNamePropertyName { get; set; } = "userName";
        public string CommandIdPropertyName { get; set; } = "commandId";
        public string QueryIdPropertyName { get; set; } = "queryId";
        public string EventIdPropertyName { get; set; } = "eventId";
    }
}