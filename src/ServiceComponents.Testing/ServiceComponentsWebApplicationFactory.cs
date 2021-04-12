using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Senders;
using ServiceComponents.Infrastructure.CorrelationContext;
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

        protected override IHostBuilder CreateHostBuilder()
        {
            return base.CreateHostBuilder()
                .ConfigureServices(services => services.AddMvc().AddApplicationPart(typeof(TEntrypoint).Assembly))
                .UseSerilog((context, configuration) => 
                {
                    if (_outputHelper == null) return;

                    configuration
                        .ReadFrom.Configuration(context.Configuration)
                        .WriteTo.TestOutput(_outputHelper);
                });
        }

        public async Task<string> SendAsync<TCommand>(TCommand command, TimeSpan timeOut)
            where TCommand : ICommand
        {
            var correlationId = Guid.NewGuid().ToString();
            await SendAsync(command, correlationId, timeOut);
            return correlationId;
        }

        public async Task<string> SendAsync<TResult>(IQuery<TResult> query, TimeSpan timeOut)
        {
            var correlationId = Guid.NewGuid().ToString();
            await SendAsync(query, correlationId, timeOut);
            return correlationId;
        }

        public async Task SendAsync<TCommand>(TCommand command, string correlationId, TimeSpan timeOut)
            where TCommand : ICommand
        {
            await SendAsync(command, correlationId, new CancellationTokenSource(timeOut).Token);
        }

        public async Task SendAsync<TResult>(IQuery<TResult> query, string correlationId, TimeSpan timeOut)
        {
            await SendAsync(query, correlationId, new CancellationTokenSource(timeOut).Token);
        }


        public async Task SendAsync<TCommand>(TCommand command, string correlationId, CancellationToken cancellationToken)
            where TCommand : ICommand
        {
            var commandSender = Services.GetRequiredService<ISendCommand>();
            var correlation = Services.GetRequiredService<Correlation>();

            correlation.CorrelationId = correlationId;

            await commandSender.SendAsync(command, cancellationToken);
        }

        public async Task SendAsync<TResult>(IQuery<TResult> query, string correlationId, CancellationToken cancellationToken)
        {
            var querySender = Services.GetRequiredService<ISendQuery>();
            var correlation = Services.GetRequiredService<Correlation>();

            correlation.CorrelationId = correlationId;

            await querySender.SendAsync(query, cancellationToken);
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
