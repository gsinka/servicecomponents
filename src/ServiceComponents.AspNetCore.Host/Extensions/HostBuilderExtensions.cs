using System;
using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using ServiceComponents.AspNet.Badge;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ServiceComponents.AspNetCore.Hosting.Extensions
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder UseBadges(this IHostBuilder builder, Action<IConfiguration, IBadgeRegistry> registrate)
        {
            builder.ConfigureContainer<ContainerBuilder>((context, builder) => {
                builder.Register(componentContext => {
                    var service = new BadgeService(componentContext.Resolve<HealthCheckService>());
                    registrate(context.Configuration, service);
                    return service;
                }).SingleInstance().AsSelf().AsImplementedInterfaces();
            });

            builder.ConfigureServices(services => services.AddMvcCore().AddApplicationPart(typeof(BadgeController).Assembly));

            return builder;
        }

        public static IHostBuilder UseOpenApi(this IHostBuilder builder, Action<SwaggerGenOptions> configure)
        {
            return builder.ConfigureServices((context, services) =>
            {
                services.AddMvcCore().AddApiExplorer();
                services.AddSwaggerGen();
            });
        }
    }
}
