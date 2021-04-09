using System;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace ServiceComponents.AspNet.Wireup
{
    public static class ServiceComponentsAspHostBuilderExtensions
    {
        public static ServiceComponentsHostBuilder UseAutofac(this ServiceComponentsHostBuilder builder)
        {
            builder.RegisterCallback(hostBuilder => hostBuilder.UseServiceProviderFactory(new AutofacServiceProviderFactory()));
            return builder;
        }

        public static ServiceComponentsHostBuilder UseSerilog(this ServiceComponentsHostBuilder builder)
        {
            builder.RegisterCallback(hostBuilder => hostBuilder.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration)));
            return builder;
        }

        public static ServiceComponentsHostBuilder UseSerilog(this ServiceComponentsHostBuilder builder, Action<HostBuilderContext, LoggerConfiguration> callback)
        {
            builder.RegisterCallback(hostBuilder => hostBuilder.UseSerilog(callback));
            return builder;
        }
        public static ServiceComponentsHostBuilder UseRequestBinder(this ServiceComponentsHostBuilder builder)
        {
            builder.RegisterCallback(options => options.UseRequestBinder());
            return builder;
        }

        public static ServiceComponentsHostBuilder AddGenericController(this ServiceComponentsHostBuilder hostBuilder, string path = default)
        {
            // TODO: solve routing to path
            return hostBuilder;
        }

        public static ServiceComponentsHostBuilder AddEndpoints(this ServiceComponentsHostBuilder builder)
        {
            builder.RegisterCallback((configuration, environment, app) => {

                app.UseEndpoints(endpoints => {

                    endpoints.MapControllers();

                    builder.EndpointRouteBuilderCallbacks.ForEach(action => {
                        action(configuration, endpoints);
                    });
                });
            });

            return builder;
        }

        public static ServiceComponentsHostBuilder AddOpenApi(this ServiceComponentsHostBuilder builder, Action<IConfiguration, SwaggerGenOptions> generatorOptions, Action<IConfiguration, SwaggerUIOptions> uiOptions)
        {
            builder.RegisterCallback((configuration, services) => {

                services.AddSwaggerGen(c => { generatorOptions(configuration, c); });

            });

            builder.RegisterCallback((configuration, environment, app) => {
                app.UseSwagger();
                app.UseSwaggerUI(c => uiOptions(configuration, c));
            });

            return builder;
        }
    }
}