using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Converters;
using Serilog;
using ServiceComponents.Application.Monitoring;
using ServiceComponents.AspNet.Badge;
using ServiceComponents.Host;
using ServiceComponents.Infrastructure.Monitoring;

namespace ReferenceApplication3.AspNet
{
    public static class Wireup
    {
        private const string LivenessTag = "Liveness";
        private const string ReadinessTag = "Readiness";
        private const string ReadinessPath = "/.well-known/ready";
        private const string LivenessPath = "/.well-known/live";

        public static IHostBuilder BuildHost(this IHostBuilder hostBuilder)
        {
            return hostBuilder
                //.UseAutofac()
                .UseSerilog();
        }

        public static IWebHostBuilder BuildWebHost(this IWebHostBuilder webHostBuilder)
        {
            return webHostBuilder;
        }

        public static IMvcBuilder BuildMvc(this IMvcBuilder mvcBuilder)
        {
            // Badge
            mvcBuilder.PartManager.ApplicationParts.Add(new AssemblyPart(typeof(BadgeController).Assembly));

            // Newtonsoft
            mvcBuilder.AddNewtonsoftJson(options => {
                options.UseCamelCasing(true);
                options.SerializerSettings.Converters.Add(new StringEnumConverter());
            });

            return mvcBuilder;
        }

        public static IServiceCollection BuildServices(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddHttpClient();
            services.AddCors();

            services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy(), new[] {LivenessTag});

            services.AddSwaggerGenNewtonsoftSupport();

            // Badge

            services.AddSingleton(provider => {
                var badgeService = new BadgeService(provider.GetRequiredService<HealthCheckService>());
                badgeService.RegistrateVersionBadge();
                badgeService.RegistrateLivenessBadge();
                badgeService.RegistrateReadinessBadge();
                return badgeService;
            });

            services.AddSingleton<IMetricsService, PrometheusMetricsService>();

            return services;
        }

        public static MvcOptions BuildMvcOptions(this MvcOptions options)
        {
            options.ModelBinderProviders.Insert(0, new RequestBinderProvider());
            return options;
        }

        public static IEndpointRouteBuilder BuildEndpointRoot(this IEndpointRouteBuilder routeBuilder, IConfiguration configuration)
        {
            // Badge

            var badgeOptions = new BadgeOptions();
            configuration.Bind(badgeOptions);

            routeBuilder.MapControllerRoute(name: "badge",
                pattern: $"{badgeOptions.Path.TrimEnd('/')}/{{name?}}",
                defaults: new { controller = "Badge", action = "Get" });

            return routeBuilder;
        }

        public static IApplicationBuilder BuildHealthCheckApp(this IApplicationBuilder app)
        {
            app.UseHealthChecks(ReadinessPath, new HealthCheckOptions {
                Predicate = registration =>
                    registration.Tags == null || !registration.Tags.Any() ||
                    registration.Tags.Contains(ReadinessTag)
            });

            app.UseHealthChecks(LivenessPath, new HealthCheckOptions {
                Predicate = registration =>
                    registration.Tags == null || !registration.Tags.Any() ||
                    registration.Tags.Contains(LivenessTag)
            });

            return app;
        }

    }
}
