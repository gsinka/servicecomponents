using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32.SafeHandles;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Infrastructure.Redis;
using StackExchange.Redis;

namespace ServiceComponents.AspNet.Wireup
{
    public static class ServiceComponentsRedisHostBuilderExtensions
    {
        public static ServiceComponentsHostBuilder AddRedis(this ServiceComponentsHostBuilder hostBuilder, string connectionString)
        {
            hostBuilder.RegisterCallback((context, containerBuilder) => {
                
                containerBuilder.AddRedisConnection(connectionString);
                containerBuilder.AddRedisDatabase();
            });

            return hostBuilder;
        }
        
        public static ServiceComponentsHostBuilder AddRedis(this ServiceComponentsHostBuilder hostBuilder, Func<IConfiguration, string> connectionStringBuilder)
        {
            hostBuilder.RegisterCallback((context, containerBuilder) => {
                
                containerBuilder.AddRedisConnection(connectionStringBuilder(context.Configuration));
                containerBuilder.AddRedisDatabase();
            });

            return hostBuilder;
        }

        public static ServiceComponentsHostBuilder AddRedisDistributedCache(this ServiceComponentsHostBuilder hostBuilder, string connection, string instance = default)
        {
            return hostBuilder.RegisterCallback((configuration, services) => services.AddStackExchangeRedisCache(options => {
                options.InstanceName = instance;
                options.Configuration = connection;
            }));
        }

        public static ServiceComponentsHostBuilder AddRedisDistributedCache(this ServiceComponentsHostBuilder hostBuilder, string connectionString)
        {
            return hostBuilder.RegisterCallback((configuration, services) => services.AddStackExchangeRedisCache(options => {
                options.ConfigurationOptions = ConfigurationOptions.Parse(connectionString);
                
            }));
        }

        public static ServiceComponentsHostBuilder AddRedisDistributedCache(this ServiceComponentsHostBuilder hostBuilder, Action<ConfigurationOptions> optionsBuilder)
        {
            var opts = new ConfigurationOptions();
            optionsBuilder(opts);

            return hostBuilder.RegisterCallback((configuration, services) => services.AddStackExchangeRedisCache(options => { options.ConfigurationOptions = opts; }));
        }
    }
}