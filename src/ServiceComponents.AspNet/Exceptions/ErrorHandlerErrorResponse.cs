using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ServiceComponents.AspNet.Exceptions
{
    /// <summary>
    /// ProblemDetails (RFC 7807) response of the exceptions of DotNet.ErrorHandler (business, not found, conflict).
    /// The property names follow what the frontend (ngx-error-handler) reads, <see cref="Data"/> keeps the
    /// original Exception.Data entries so the exception can be restored on the BFF
    /// </summary>
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ErrorHandlerErrorResponse : IErrorResponse
    {
        public const string ContentType = "application/problem+json";

        [JsonProperty("type")]
        public string Type { get; set; } = "about:blank";

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("detail")]
        public string Detail { get; set; }

        [JsonProperty("errorCode")]
        public string ErrorCode { get; set; }

        [JsonProperty("contextCode")]
        public string ContextCode { get; set; }

        [JsonProperty("hint")]
        public string Hint { get; set; }

        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }

        [JsonProperty("traceId")]
        public string TraceId { get; set; }

        [JsonProperty("iServiceErrorObject")]
        public ErrorHandlerServiceError IServiceErrorObject { get; set; }

        [JsonProperty("executionSteps")]
        public JToken ExecutionSteps { get; set; }

        [JsonProperty("helpUrls")]
        public JToken HelpUrls { get; set; }

        [JsonProperty("data")]
        public IDictionary<string, JToken> Data { get; set; } = new Dictionary<string, JToken>();

        [JsonIgnore]
        public string ErrorMessage => Detail;
    }

    /// <summary>
    /// The error of the iService behind the exception. Everything but the text and the hint (module, source, line...)
    /// is in <see cref="Extra"/> and goes to the frontend as it is
    /// </summary>
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class ErrorHandlerServiceError
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("hint")]
        public string Hint { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JToken> Extra { get; set; } = new Dictionary<string, JToken>();
    }
}
