using NSwag;

namespace ServiceComponents.AspNet.OpenApi
{
    public class OpenApiSsoOptions
    {
        public string SecuritySchemeName { get; set; } = "OAuth2";
        public string SecurityScheme { get; set; } = "http";
        public OpenApiSecuritySchemeType SecuritySchemeType { get; set; } = OpenApiSecuritySchemeType.OAuth2;
        public OpenApiOAuth2Flow OAuthFlow { get; set; } = OpenApiOAuth2Flow.Implicit;
        public string Authority { get; set; } = "http://localhost";
    }
}