using System.Reflection;
using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using ServiceComponents.AspNet.Controllers;
using ServiceComponents.AspNet.Http;
using ServiceComponents.Core;

namespace ServiceComponents.AspNet.Wireup
{
    public static class ServiceComponentsDefaultHostBuilderExtensions
    {
        public static ServiceComponentsHostBuilder UseDefault(this ServiceComponentsHostBuilder builder, Assembly[] apiAssemblies, Assembly[] applicationAssemblies)
        {
            return builder

                    // Use Autofac for dependency injection
                    .UseAutofac()

                    // Use serilog for logging
                    .UseSerilog((context, log) => log
                        .WriteTo.Console()
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                        .MinimumLevel.Verbose())

                    .RegisterCallback((configuration, environment, app) => {
                        
                        if (environment.IsDevelopment()) {
                            app.UseDeveloperExceptionPage();
                        }

                        app.UseRouting();
                        app.UseAuthorization();
                    })

                    // Add request binder for command, query and event deserialization
                    .UseRequestBinder()

                    .AddGenericController("api/generic")

                    .RegisterCallback((configuration, services) => {
                        var mvcBuilder = services.AddControllers(options => {
                            builder.MvcOptionsBuilderCallbacks.ForEach(action => action(options));
                        });

                        var hostAssembly = Assembly.GetEntryAssembly();
                        mvcBuilder.PartManager.ApplicationParts.Add(new AssemblyPart(hostAssembly));
                        mvcBuilder.PartManager.ApplicationParts.Add(new AssemblyPart(typeof(GenericController).Assembly));
                        builder.MvcBuilderCallbacks.ForEach(x => x(mvcBuilder));

                        services.AddHttpContextAccessor();
                        services.AddHttpClient();
                    })
                    
                    // Add OpenApi
                    .AddOpenApi()
                    
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

                    // Add endpoints
                    .AddEndpoints()


                ;
        }
    }
}