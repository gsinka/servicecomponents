using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using ReferenceApplication.AspNet.Wireup;
using Serilog;

namespace ReferenceApplication.AspNet
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            
            Log.Information("Ref-app starting");
            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>

            Host.CreateDefaultBuilder(args)

                // Inject host builder wireup
                .WireupServiceComponents(args)

                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        // Inject web host builder wireup
                        .WireupServiceComponents(args)
                        .UseStartup<Startup>();
                });
    }
}
