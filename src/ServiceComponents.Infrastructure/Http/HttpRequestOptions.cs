namespace ServiceComponents.Infrastructure.Http
{
    public class HttpRequestOptions
    {
        public string DomainTypeHeaderKey { get; set; } = "domain-type";
        public string CorrelationIdHeaderKey { get; set; } = "correlation-id";
        public string CausationIdHeaderKey { get; set; } = "causation-id";
    }

}
