using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace ReferenceApplication.AspNet.Wireup.Extensions
{
    public static class HealthcheckExtensions
    {
        public const string LivenessTag = "Liveness";
        public const string ReadinessTag = "Readiness";

        public static IApplicationBuilder AddReadiness(this IApplicationBuilder app, string path = "/.well-known/ready")
        {
            return app.UseHealthChecks(path, new HealthCheckOptions {
                Predicate = registration =>
                    registration.Tags == null || !registration.Tags.Any() ||
                    registration.Tags.Contains(ReadinessTag)
            });

        }

        public static IApplicationBuilder AddLiveness(this IApplicationBuilder app, string path = "/.well-known/live")
        {
            return app.UseHealthChecks(path, new HealthCheckOptions {
                Predicate = registration =>
                    registration.Tags == null || !registration.Tags.Any() ||
                    registration.Tags.Contains(LivenessTag)
            });
        }
    }
}
