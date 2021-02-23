using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSwag;
using NSwag.AspNetCore;
using NSwag.Generation.Processors.Security;
using ServiceComponents.AspNet.OpenApi;
using ServiceComponents.Core.ExtensionMethods;

namespace ReferenceApplication.AspNet.Wireup
{
    public static class OpenApiServiceExtensions
    {
        public static IServiceCollection ConfigureOpenApi(this IServiceCollection services, IConfiguration configuration)
        {
            var documentOptions = new OpenApiDocumentOptions();
            configuration.GetSection("openAPiDocument").Bind(documentOptions);

            var ssoOptions = new OpenApiSsoOptions();
            configuration.GetSection("openAPiSso").Bind(ssoOptions);
            
            services.AddOpenApiDocument(options => {
                options.DocumentName = documentOptions.DocumentName;
                options.Title = documentOptions.AppTitle;
                options.Version = documentOptions.AppVersion;
                options.AllowReferencesWithProperties = true;
                options.OperationProcessors.Add(new OperationSecurityScopeProcessor(ssoOptions.SecuritySchemeName));

                options.DocumentProcessors.Add(new SecurityDefinitionAppender(ssoOptions.SecuritySchemeName, new OpenApiSecurityScheme {
                    Name = ssoOptions.SecuritySchemeName,
                    Scheme = ssoOptions.SecurityScheme,
                    Type = ssoOptions.SecuritySchemeType,
                    Flow = ssoOptions.OAuthFlow,
                    AuthorizationUrl = $"{ssoOptions.Authority}/protocol/openid-connect/auth",
                    Scopes = new Dictionary<string, string>
                    {
                        { "openid", "Open ID" },
                        { "profile", "Profile" }
                    }
                }));

            });
            
            return services;
        }

        public static IApplicationBuilder ConfigureOpenApi(this IApplicationBuilder app, IConfiguration configuration)
        {
            var openApiOptions = new OpenApiOptions();
            configuration.GetSection("openAPi").Bind(openApiOptions);
            
            app.UseOpenApi(settings => { settings.DocumentName = openApiOptions.DocumentName; });

            app.UseSwaggerUi3(settings => {
                settings.Path = openApiOptions.Path;
                settings.OAuth2Client = new OAuth2ClientSettings() {
                    AppName = openApiOptions.AppName,
                    ClientId = openApiOptions.ClientId
                };
                settings.OAuth2Client.AdditionalQueryStringParameters.Add("nonce", StringHelper.Random(6, RandomCharacterSet.AlphanumericAndSpecial));
            });

            return app;
        }
    }
}
