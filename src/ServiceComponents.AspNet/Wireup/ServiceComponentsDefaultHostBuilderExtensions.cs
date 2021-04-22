using System;
using System.Reflection;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServiceComponents.AspNet.Badge;
using ServiceComponents.AspNet.Http;
using ServiceComponents.Core.Services;

namespace ServiceComponents.AspNet.Wireup
{
    public static class ServiceComponentsDefaultHostBuilderExtensions
    {
        public static ServiceComponentsHostBuilder ConfigureApp(this ServiceComponentsHostBuilder hostBuilder, Action<IConfiguration, IHostEnvironment, IApplicationBuilder> appBuilder)
        {
            return hostBuilder.RegisterCallback(appBuilder);
        }

        public static ServiceComponentsHostBuilder ConfigureMvc(this ServiceComponentsHostBuilder hostBuilder, Action<IMvcBuilder> mvcBuilder)
        {
            return hostBuilder.RegisterCallback(mvcBuilder);
        }
        public static ServiceComponentsHostBuilder ConfigureContainer(this ServiceComponentsHostBuilder hostBuilder, Action<HostBuilderContext, ContainerBuilder> containerBuilder)
        {
            return hostBuilder.RegisterCallback(containerBuilder);
        }

        public static ServiceComponentsHostBuilder UseDefault(this ServiceComponentsHostBuilder builder, Assembly[] apiAssemblies, Assembly[] applicationAssemblies)
        {
            return builder

                    // Use Autofac for dependency injection
                    .UseAutofac()
                    
                    // Add request binder for command, query and event deserialization
                    .UseRequestBinder()

                    .RegisterCallback((configuration, services) => {

                        var mvcBuilder = services.AddControllers(options => {
                            builder.MvcOptionsBuilderCallbacks.ForEach(action => action(options));
                        });

                        var hostAssembly = Assembly.GetEntryAssembly();
                        mvcBuilder.PartManager.ApplicationParts.Add(new AssemblyPart(hostAssembly));
                        mvcBuilder.PartManager.ApplicationParts.Add(new AssemblyPart(typeof(BadgeController).Assembly));
                        builder.MvcBuilderCallbacks.ForEach(x => x(mvcBuilder));

                        services.AddHttpContextAccessor();
                        services.AddHttpClient();
                        services.AddCors();
                    })

                    .RegisterCallback((context, containerBuilder) => {

                        containerBuilder.RegisterType<ComputerClock>().AsImplementedInterfaces().SingleInstance();
                        containerBuilder.AddHttpRequestParser();
                    })

                    // Add Mediator components
                    .AddMediator(applicationAssemblies, addBehavior: true)

                    // Add fluent validation behavior
                    .AddValidationBehavior(apiAssemblies)

                    // Add request logging behavior
                    .AddLogBehavior()

                    // Add stopwatch behavior for measure and log execution time of command, query and event handlers
                    .AddStopwatchBehavior()

                    // Registers CorrelationInfo service
                    .AddCorrelation()

                    // Register receivers
                    .AddReceivers()

                    // Add loopback command, query and event senders with key 'loopback'
                    .AddLoopbackSender("loopback")

                ;
        }
    }
}