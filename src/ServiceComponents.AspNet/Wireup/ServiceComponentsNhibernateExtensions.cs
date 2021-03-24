using System;
using Autofac;
using FluentNHibernate.Cfg;
using Microsoft.Extensions.Configuration;
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
        
        public static ServiceComponentsHostBuilder AddNHibernate(this ServiceComponentsHostBuilder hostBuilder, Func<IConfiguration, string> connectionString, Action<MappingConfiguration> mapping, Action<Configuration> exposeConfiguration)
        {
            return hostBuilder.RegisterCallback((context, containerBuilder) => {
                containerBuilder.RegisterModule(new NhibernateModule(connectionString(context.Configuration), mapping, exposeConfiguration));
            });
        }
    }
}