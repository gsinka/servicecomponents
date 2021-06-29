using System;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

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
