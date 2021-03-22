using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Infrastructure.Redis;

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

        public static ServiceComponentsHostBuilder AddRedisCommandRules(this ServiceComponentsHostBuilder hostBuilder, Func<ICommand, IList<ICommand>, bool> constraint, Func<ICommand, TimeSpan?> expiryFunc = default)
        {
            hostBuilder.RegisterCallback((context, containerBuilder) => { containerBuilder.AddRedisCommandConstraints(constraint, expiryFunc); });
            return hostBuilder;
        }
    }
}