using System;
using System.Net.Http;
using Autofac;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServiceComponents.Testing.Extensions;
using Xunit.Abstractions;

namespace ServiceComponents.Testing
{
    public abstract class TestHost<TEntrypoint> : WebApplicationFactory<TEntrypoint>, ITestHost
        where TEntrypoint : class
    {
        public ITestOutputHelper OutputHelper { get; set; }

        protected override IHostBuilder CreateHostBuilder()
        {
            return base.CreateHostBuilder()
                .UseOutputHelper(OutputHelper)
                .ConfigureServices(ConfigureTestServices)
                .ConfigureContainer<ContainerBuilder>(ConfigureTestContainer);
        }

        public abstract void ConfigureTestServices(HostBuilderContext context, IServiceCollection services);
        public abstract void ConfigureTestContainer(HostBuilderContext context, ContainerBuilder container);
    }

    public interface ITestHost : IDisposable
    {
        ITestOutputHelper OutputHelper { set; }
        HttpClient CreateClient();

    }
}
