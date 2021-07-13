using System;
using Microsoft.Extensions.Hosting;
using Serilog;
using Xunit.Abstractions;

namespace ServiceComponents.Testing.Extensions
{
    public static class HostBuilderExtensions
    {
        public static IHostBuilder UseOutputHelper(this IHostBuilder builder, ITestOutputHelper outputHelper)
        {
            if (outputHelper is null) {
                throw new ArgumentNullException(nameof(outputHelper), "Set TestHost.OutputHelper property");
            }

            return builder.UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration)
                                                                               .WriteTo.Seq("http://localhost:5341", Serilog.Events.LogEventLevel.Verbose)
                                                                               .WriteTo.TestOutput(outputHelper).MinimumLevel.Verbose());
        }
    }
}
