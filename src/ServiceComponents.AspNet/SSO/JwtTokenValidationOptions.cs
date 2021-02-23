namespace ServiceComponents.AspNet.SSO
{
    public class JwtTokenValidationOptions
    {
        public string Authority { get; set; }
        public string Audience { get; set; } = "account";
        public bool RequireHttpsMetadata { get; set; } = true;
        public bool SaveToken { get; set; } = true;
        public bool ShowPII { get; set; } = false;
    }
}