using System;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using ServiceComponents.AspNetCore.Hosting.Extensions;
using ServiceComponents.AspNetCore.Hosting.Options;

namespace ServiceComponents.AspNetCore.Hosting
{
    public static class ServiceComponentsHost
    {
        public static IHostBuilder CreateDefaultHost<TWireup>()
            where TWireup : Wireup
        {
            var host = Host.CreateDefaultBuilder();
            var wireup = Activator.CreateInstance<TWireup>();

            host
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())

                .UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration))

                .UseBadges(wireup.ConfigureBadges)

                .UseOpenApi(wireup.ConfigureSwagger)

                .ConfigureServices(services => services.AddOptions<ApplicationOptions>().BindConfiguration("Application").ValidateDataAnnotations())
                .ConfigureServices(services => services.AddMvcCore().AddApplicationPart(wireup.GetType().Assembly))

                .ConfigureServices(services => wireup.RegistrateHealthChecks(services.AddHealthChecks()))

                .ConfigureServices(wireup.ConfigureServices)
                .ConfigureContainer<ContainerBuilder>(wireup.ConfigureContainer)

                .ConfigureWebHostDefaults(webHostBuilder => {
                    var pipe = new RequestPipe();
                    wireup.PrepareRequestPipe(pipe);

                    webHostBuilder.Configure((context, builder) => {
                        foreach (var item in pipe) {
                            item.Use(context, builder);
                        }
                    });
                });

                
            return host;
        }
    }
}
