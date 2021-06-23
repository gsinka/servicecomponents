using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace ServiceComponents.AspNetCore.Hosting.Extensions
{
    public static class SwaggerExtensions
    {
        public static SwaggerUIOptions OpenIdConnect(this SwaggerUIOptions options, string client)
        {
            options.OAuthClientId(client);
            options.OAuthScopes("openid", "profile");

            return options;
        }

        public static SwaggerUIOptions Endpoint(this SwaggerUIOptions options, string name)
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", name);

            return options;
        }

        public static SwaggerGenOptions ConfigureSwaggerDoc(this SwaggerGenOptions options, string name, OpenApiInfo info)
        {
            options.SwaggerDoc(name, info);

            return options;
        }

        public static SwaggerGenOptions IncludeAppXml(this SwaggerGenOptions options)
        {
            var appXmlDoc = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetEntryAssembly().GetName().Name}.xml");
            options.IncludeXmlComments(appXmlDoc);

            return options;
        }

        public static SwaggerGenOptions AddAuthentication(this SwaggerGenOptions options, string url)
        {
            options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme() {
                Type = SecuritySchemeType.OpenIdConnect,
                OpenIdConnectUrl = new Uri(url),
                In = ParameterLocation.Header,
                BearerFormat = "JWT",
                Scheme = JwtBearerDefaults.AuthenticationScheme
            });

            return options;
        }
    }
}
