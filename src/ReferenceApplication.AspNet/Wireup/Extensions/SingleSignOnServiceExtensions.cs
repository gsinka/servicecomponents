using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using ServiceComponents.AspNet.SSO;

namespace ReferenceApplication.AspNet.Wireup.Extensions
{
    public static class SingleSignOnServiceExtensions
    {
        public static IServiceCollection ConfigureSingleSignOn(this IServiceCollection services, IConfiguration configuration)
        {
            // Single sign on

            services.Configure<JwtTokenValidationOptions>(configuration.GetSection("singleSignOn"));

            var ssoOptions = new JwtTokenValidationOptions();
            configuration.GetSection("singleSignOn").Bind(ssoOptions);

            IdentityModelEventSource.ShowPII = ssoOptions.ShowPII;

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options => {
                options.MetadataAddress = $"{ssoOptions.Authority}/.well-known/openid-configuration";
                options.Audience = ssoOptions.Audience;
                options.RequireHttpsMetadata = ssoOptions.RequireHttpsMetadata;
                options.SaveToken = ssoOptions.SaveToken;
                options.TokenValidationParameters = new TokenValidationParameters() {
                };
            });

            services.AddAuthorization(options => {
                options.AddPolicy("admin", builder => builder.RequireClaim("user-roles", new[] { "administrator" }));
            });

            return services;
        }
    }
}