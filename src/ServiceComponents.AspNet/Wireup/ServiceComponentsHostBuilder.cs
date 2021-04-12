using System;
using System.Collections.Generic;
using System.Drawing;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;

namespace ServiceComponents.AspNet.Wireup
{
    public class ServiceComponentsHostBuilder
    { 
        private readonly List<Action<IHostBuilder>> _hostBuilderCallbacks = new List<Action<IHostBuilder>>();
        private readonly List<Action<IConfiguration, IServiceCollection>> _serviceCollectionCallbacks = new List<Action<IConfiguration, IServiceCollection>>();
        public readonly List<Action<MvcOptions>> MvcOptionsBuilderCallbacks = new List<Action<MvcOptions>>();
        public readonly List<Action<IMvcBuilder>> MvcBuilderCallbacks = new List<Action<IMvcBuilder>>();
        public readonly List<Action<IConfiguration, IHostEnvironment, IApplicationBuilder>> ApplicationBuilderCallbacks = new List<Action<IConfiguration, IHostEnvironment, IApplicationBuilder>>();
        public readonly List<Action<HostBuilderContext, ContainerBuilder>> ContainerBuilderCallbacks = new List<Action<HostBuilderContext, ContainerBuilder>>();
        public readonly List<Action<IConfiguration, ILifetimeScope>> StartupCallbacks = new List<Action<IConfiguration, ILifetimeScope>>();
        public readonly List<Action<IConfiguration, IEndpointRouteBuilder>> EndpointRouteBuilderCallbacks = new List<Action<IConfiguration, IEndpointRouteBuilder>>();


        public ServiceComponentsHostBuilder RegisterCallback(Action<IHostBuilder> callback)
        {
            _hostBuilderCallbacks.Add(callback);
            return this;
        }
        public ServiceComponentsHostBuilder RegisterCallback(Action<HostBuilderContext, ContainerBuilder> callback)
        {
            ContainerBuilderCallbacks.Add(callback);
            return this;
        }

        public ServiceComponentsHostBuilder RegisterCallback(Action<IConfiguration, IServiceCollection> callback)
        {
            _serviceCollectionCallbacks.Add(callback);
            return this;
        }

        public ServiceComponentsHostBuilder RegisterCallback(Action<MvcOptions> callback)
        {
            MvcOptionsBuilderCallbacks.Add(callback);
            return this;
        }
        
        public ServiceComponentsHostBuilder RegisterCallback(Action<IMvcBuilder> callback)
        {
            MvcBuilderCallbacks.Add(callback);
            return this;
        }
        
        public ServiceComponentsHostBuilder RegisterCallback(Action<IConfiguration, ILifetimeScope> callback)
        {
            StartupCallbacks.Add(callback);
            return this;
        }
        
        public ServiceComponentsHostBuilder RegisterCallback(Action<IConfiguration, IEndpointRouteBuilder> callback)
        {
            EndpointRouteBuilderCallbacks.Add(callback);
            return this;
        }

        public ServiceComponentsHostBuilder RegisterCallback(Action<IConfiguration, IHostEnvironment, IApplicationBuilder> callback)
        {
            ApplicationBuilderCallbacks.Add(callback);
            return this;
        }

        public IHostBuilder CreateHostBuilder(string[] args)
        {
            var hostBuilder = Host.CreateDefaultBuilder(args);

            _hostBuilderCallbacks.ForEach(action => action(hostBuilder));

            hostBuilder.ConfigureContainer<ContainerBuilder>((context, builder) => ContainerBuilderCallbacks.ForEach(action => action(context, builder)));

            hostBuilder.ConfigureWebHostDefaults(webHostBuilder => {
                webHostBuilder
                    .ConfigureServices((context, services) => _serviceCollectionCallbacks.ForEach(action => action(context.Configuration, services)))
                    .Configure((context, app) => ApplicationBuilderCallbacks.ForEach(action => action(context.Configuration, context.HostingEnvironment, app)));
            });

            return hostBuilder;
        }
    }
}
