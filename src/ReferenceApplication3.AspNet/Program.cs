using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using ServiceComponents.Host;

namespace ReferenceApplication3.AspNet
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
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception exception) {
                Log.Fatal(exception, "Host terminated unexpectedly");
            }
            finally {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host
                .CreateDefaultBuilder(args)
                
                .BuildHost() // -> Wireup

                .ConfigureWebHostDefaults(webBuilder => {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.BuildWebHost(); // -> Wireup
                });

    }
}
