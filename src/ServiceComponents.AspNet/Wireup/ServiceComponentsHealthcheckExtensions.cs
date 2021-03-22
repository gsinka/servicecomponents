using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ServiceComponents.AspNet.Wireup
{
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