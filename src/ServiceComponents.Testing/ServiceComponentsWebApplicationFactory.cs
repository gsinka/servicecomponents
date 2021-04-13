using System.Net.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using Xunit.Abstractions;
using HttpRequestOptions = ServiceComponents.AspNet.Http.HttpRequestOptions;

namespace ServiceComponents.Testing
{
    public class ServiceComponentsWebApplicationFactory<TEntrypoint> : WebApplicationFactory<TEntrypoint>
        where TEntrypoint : class
    {
        private ITestOutputHelper _outputHelper;

        public void SetOutputHelper(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureServices(services => services.AddMvc().AddApplicationPart(typeof(TEntrypoint).Assembly))
                .UseSerilog((context, configuration) => {
                    if (_outputHelper == null) return;

                    configuration
                        .ReadFrom.Configuration(context.Configuration)
                        .WriteTo.TestOutput(_outputHelper);
                });
        }

        public HttpClient CreateClient(string correlationId)
        {
            var options = Services.GetRequiredService<IOptions<HttpRequestOptions>>();
            var result = CreateClient();
            result.DefaultRequestHeaders.Add(options.Value.CorrelationIdHeaderKey, correlationId);

            return result;
        }
    }
}
