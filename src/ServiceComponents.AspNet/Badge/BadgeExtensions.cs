using System;
using Autofac;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ServiceComponents.AspNet.Badge
{
    public static class AutofacBadgeExtensions
    {
        public static ContainerBuilder AddBadgeService(this ContainerBuilder builder, Action<IBadgeRegistry> registrate)
        {
            builder.Register(context => {
                var service = new BadgeService(context.Resolve<HealthCheckService>());
                registrate(service);
                return service;
            }).SingleInstance().AsSelf().AsImplementedInterfaces();
            return builder;
        }
    }
}
