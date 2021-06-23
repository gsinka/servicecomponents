using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using ServiceComponents.AspNetCore.Hosting;
using ServiceComponents.AspNetCore.Hosting.Extensions;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace ReferenceApplication2.AspNet
{
    public class AppWireup : Wireup
    {
        protected override void ConfigureAppSwagger(SwaggerGenOptions swaggerGenOptions)
        {
            swaggerGenOptions.ConfigureSwaggerDoc("v1", new OpenApiInfo { Title = "Reference Application2", Version = "v1" })
                .IncludeAppXml()
                .AddAuthentication("http://localhost:8080/auth/realms/develop/.well-known/openid-configuration");
        }

        protected override void ConfigureAppSwaggerUi(SwaggerUIOptions options)
        {
            options.Endpoint("Reference Application 2")
                .OpenIdConnect("reference-application");
        }

        protected override void ConfigureContainer(HostBuilderContext context, ContainerBuilder builder)
        {
            
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            
        }

        protected override void RegistrateHealthChecks(IHealthChecksBuilder builder)
        {
            
        }

        protected override void PrepareRequestPipe(RequestPipe pipe)
        {
            base.PrepareRequestPipe(pipe);

            pipe.
        }
    }
}
