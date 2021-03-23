using System;
using Autofac;
using FluentNHibernate.Cfg;
using NHibernate.Cfg;
using ServiceComponents.Infrastructure.NHibernate;

namespace ServiceComponents.AspNet.Wireup
{
    public static class ServiceComponentsNhibernateExtensions
    {
        public static ServiceComponentsHostBuilder AddNHibernate(this ServiceComponentsHostBuilder hostBuilder, string connectionString, Action<MappingConfiguration> mapping, Action<Configuration> exposeConfiguration)
        {
            return hostBuilder.RegisterCallback((context, containerBuilder) => {
                containerBuilder.RegisterModule(new NhibernateModule(connectionString, mapping, exposeConfiguration));
            });
        }
    }
}