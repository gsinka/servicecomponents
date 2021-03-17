using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Autofac.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.EventLog;

namespace ServiceComponents.AspNet.Wireup
{
    public static class ServiceComponentsHostBuilder
    {
        public static IHostBuilder CreateBuilder() => CreateBuilder(args: null);

        public static IHostBuilder CreateBuilder(string[] args)
        {
            var builder = new HostBuilder();

            builder.UseContentRoot(Directory.GetCurrentDirectory());

            builder.ConfigureHostConfiguration(config => {
                config.AddEnvironmentVariables(prefix: "DOTNET_");
                if (args != null) {
                    config.AddCommandLine(args);
                }
            });

            builder.ConfigureAppConfiguration((hostingContext, config) => {
                IHostEnvironment env = hostingContext.HostingEnvironment;

                bool reloadOnChange = hostingContext.Configuration.GetValue("hostBuilder:reloadConfigOnChange", defaultValue: true);

                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: reloadOnChange)
                      .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: reloadOnChange);

                if (env.IsDevelopment() && !string.IsNullOrEmpty(env.ApplicationName)) {
                    var appAssembly = Assembly.Load(new AssemblyName(env.ApplicationName));
                    if (appAssembly != null) {
                        config.AddUserSecrets(appAssembly, optional: true, reloadOnChange: reloadOnChange);
                    }
                }

                config.AddEnvironmentVariables();

                if (args != null) {
                    config.AddCommandLine(args);
                }
            })
                .ConfigureLogging((hostingContext, logging) => {
                
                    bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

                    // IMPORTANT: This needs to be added *before* configuration is loaded, this lets
                    // the defaults be overridden by the configuration.
                    if (isWindows) {
                        // Default the EventLogLoggerProvider to warning or above
                        logging.AddFilter<EventLogLoggerProvider>(level => level >= LogLevel.Warning);
                    }

                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                    logging.AddDebug();
                    logging.AddEventSourceLogger();

                    if (isWindows) {
                        // Add the EventLogLoggerProvider on windows machines
                        logging.AddEventLog();
                    }

                    logging.Configure(options => {
                        options.ActivityTrackingOptions = ActivityTrackingOptions.SpanId
                                                          | ActivityTrackingOptions.TraceId
                                                          | ActivityTrackingOptions.ParentId;
                    });

                })

            .UseDefaultServiceProvider((context, options) => {
                bool isDevelopment = context.HostingEnvironment.IsDevelopment();
                options.ValidateScopes = isDevelopment;
                options.ValidateOnBuild = isDevelopment;
            });

            return builder;

        }

    }
}