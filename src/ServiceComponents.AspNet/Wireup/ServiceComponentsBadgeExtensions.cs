using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using ServiceComponents.AspNet.Badge;

namespace ServiceComponents.AspNet.Wireup
{
    public static class ServiceComponentsBadgeExtensions
    {

        public static ServiceComponentsHostBuilder AddBadge(this ServiceComponentsHostBuilder hostBuilder, Action<IConfiguration, IBadgeRegistry> builder, string pattern = ".badge")
        {
            return hostBuilder.RegisterCallback((context, containerBuilder) => {
                    containerBuilder.AddBadgeService(registry => builder(context.Configuration, registry));
                })
                .RegisterCallback((configuration, routeBuilder) => {
                    routeBuilder.MapControllerRoute(name: "badge",
                        pattern: $"{pattern.TrimEnd('/')}/{{name?}}",
                        defaults: new {controller = "Badge", action = "Get"});
                });
        }

        public static ServiceComponentsHostBuilder AddBadge(this ServiceComponentsHostBuilder hostBuilder)
        {
            return AddBadge(hostBuilder, (configuration, registry) => {
                registry.RegistrateLivenessBadge();
                registry.RegistrateReadinessBadge();
                registry.RegistrateVersionBadge();
            });
        }
    }
}