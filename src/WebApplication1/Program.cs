using System;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using ServiceComponents.Infrastructure.CouchDB;

namespace WebApplication1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Debug()
                .WriteTo.Console(LogEventLevel.Information)
                .MinimumLevel.Override("NHibernate", LogEventLevel.Warning)
                .MinimumLevel.Override("NHibernate.SQL", LogEventLevel.Warning)
                .CreateBootstrapLogger();

            try {
                Log.Information("Starting service web host");
                var host = CreateHostBuilder(args).Build();
                CustomStartup.Initialize(host);
                host.Run();
            }
            catch (Exception exception) {
                Log.Fatal(exception, "Host terminated unexpectedly");
            }
            finally {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)

                //// CouchDB configuration
                //.ConfigureAppConfiguration((context, config) => {
                //    var temp = config.Build();
                //    var serviceConfiguration = temp.GetSection("ServiceConfiguration").Get<ServiceConfigurationOptions>();
                //    config.Sources.Insert(0, new CouchDbConfigurationSource(serviceConfiguration));
                //})

                // Autofac service provider
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())

                // Serilog integration
                .UseSerilog(CustomStartup.ConfigureSerilog)

                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
