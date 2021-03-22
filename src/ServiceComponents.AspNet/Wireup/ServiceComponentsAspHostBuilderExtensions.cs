using System;
using System.Linq;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Serilog;

namespace ServiceComponents.AspNet.Wireup
{
    public static class ServiceComponentsAspHostBuilderExtensions
    {
        public static ServiceComponentsHostBuilder UseAutofac(this ServiceComponentsHostBuilder builder)
        {
            builder.RegisterCallback(hostBuilder => hostBuilder.UseServiceProviderFactory(new AutofacServiceProviderFactory()));
            return builder;
        }

        public static ServiceComponentsHostBuilder UseSerilog(this ServiceComponentsHostBuilder builder)
        {
            builder.RegisterCallback(hostBuilder => hostBuilder.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration)));
            return builder;
        }

        public static ServiceComponentsHostBuilder UseSerilog(this ServiceComponentsHostBuilder builder, Action<HostBuilderContext, LoggerConfiguration> callback)
        {
            builder.RegisterCallback(hostBuilder => hostBuilder.UseSerilog(callback));
            return builder;
        }
        public static ServiceComponentsHostBuilder UseRequestBinder(this ServiceComponentsHostBuilder builder)
        {
            builder.RegisterCallback(options => options.UseRequestBinder());
            return builder;
        }

        public static ServiceComponentsHostBuilder AddGenericController(this ServiceComponentsHostBuilder hostBuilder, string path = default)
        {
            // TODO: solve routing to path
            return hostBuilder;
        }

        public static ServiceComponentsHostBuilder AddEndpoints(this ServiceComponentsHostBuilder builder)
        {
            builder.RegisterCallback((configuration, environment, app) => {

                app.UseEndpoints(endpoints => {
                    endpoints.MapControllers();
                });
            });

            return builder;
        }

        public static ServiceComponentsHostBuilder AddOpenApi(this ServiceComponentsHostBuilder builder)
        {
            builder.RegisterCallback((configuration, services) => {

                services.AddSwaggerGen(c => {
                    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ReferenceApplication2.AspNet", Version = "v1" });
                });

            });

            builder.RegisterCallback((configuration, environment, app) => {
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ReferenceApplication2.AspNet v1"));
            });

            return builder;
        }
    }

    public static class ServiceComponentsHealthcheckExtensions
    {
        public const string LivenessTag = "Liveness";
        public const string ReadinessTag = "Readiness";

        public static ServiceComponentsHostBuilder AddHealthCheck(
            this ServiceComponentsHostBuilder hostBuilder,
            Action<IConfiguration, IHealthChecksBuilder> builder,
            string readinessPath = "/.well-known/ready", 
            string livenessPath = "/.well-known/live")
        {
            return hostBuilder
                
                .RegisterCallback((configuration, environment, app) => {

                app.UseHealthChecks(readinessPath, new HealthCheckOptions {
                    Predicate = registration =>
                        registration.Tags == null || !registration.Tags.Any() ||
                        registration.Tags.Contains(ReadinessTag)
                });

                app.UseHealthChecks(livenessPath, new HealthCheckOptions {
                    Predicate = registration =>
                        registration.Tags == null || !registration.Tags.Any() ||
                        registration.Tags.Contains(LivenessTag)
                });
            })
                .RegisterCallback((configuration, services) => {

                    builder(configuration, services.AddHealthChecks()
                        .AddCheck("self", () => HealthCheckResult.Healthy(), new[] { LivenessTag }));

                });

        }
    }
}