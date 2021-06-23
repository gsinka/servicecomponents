using System;
using System.IO;
using Autofac;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using ServiceComponents.AspNet.Badge;
using ServiceComponents.AspNet.Monitoring;
using ServiceComponents.AspNetCore.Hosting.Items;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace ServiceComponents.AspNetCore.Hosting
{
    public abstract class Wireup
    {
        protected internal abstract void ConfigureServices(IServiceCollection services);
        protected internal abstract void ConfigureContainer(HostBuilderContext context, ContainerBuilder builder);
        protected internal abstract void RegistrateHealthChecks(IHealthChecksBuilder builder);

        protected internal virtual void ConfigureBadges(IConfiguration configuration, IBadgeRegistry registry)
        {
            registry.RegistrateVersionBadge();
            registry.RegistrateLivenessBadge();
            registry.RegistrateReadinessBadge();
        }

        protected internal virtual void PrepareRequestPipe(RequestPipe pipe)
        {
            pipe.Add(new RoutingPipeItem());
            pipe.Add(new SwaggerPipeItem(ConfigureSwaggerUi));

            pipe.Add(new EndpointPipeItem(ConfigureEndpoints));
        }

        protected internal virtual void ConfigureSwaggerUi(SwaggerUIOptions options)
        {
            ConfigureAppSwaggerUi(options);
        }

        protected internal virtual void ConfigureEndpoints(IEndpointRouteBuilder builder)
        {
            builder.MapControllers();
            builder.MapControllerRoute(name: "badge",
                        pattern: $".badge/{{name?}}",
                        defaults: new { controller = "Badge", action = "Get" });
        }

        internal void ConfigureSwagger(SwaggerGenOptions options)
        {
            var aspNetXmlDoc = Path.Combine(AppContext.BaseDirectory, $"{typeof(MetricsController).Assembly.GetName().Name}.xml");
            options.IncludeXmlComments(aspNetXmlDoc);

            options.OperationFilter<SecurityRequirementsOperationFilter>(true, JwtBearerDefaults.AuthenticationScheme);

            ConfigureAppSwagger(options);
        }

        protected internal abstract void ConfigureAppSwagger(SwaggerGenOptions option);
        protected internal abstract void ConfigureAppSwaggerUi(SwaggerUIOptions options);
    }
}
