namespace ServiceComponents.AspNet.OpenApi
{
    public class OpenApiOptions
    {
        public string DocumentName { get; set; } = "Document";
        public string Path { get; set; } = "/swagger";

        public string AppName { get; set; } = "APP";
        public string ClientId { get; set; } = "ClientId";
    }
}
