using System;
using System.Net.Http;
using Autofac;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServiceComponents.Testing.Extensions;
using ServiceComponents.Testing.Workarounds;
using Xunit.Abstractions;

namespace ServiceComponents.Testing
{
    public abstract class TestHost<TEntrypoint> : WebApplicationFactory<TEntrypoint>, ITestHost
        where TEntrypoint : class
    {
        public ITestOutputHelper OutputHelper { get; set; }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.UseServiceProviderFactory(new CustomServiceProviderFactory());
            builder.UseOutputHelper(OutputHelper);
            return base.CreateHost(builder);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureTestServices(ConfigureTestServices);
            builder.ConfigureTestContainer<ContainerBuilder>(ConfigureTestContainer);
            base.ConfigureWebHost(builder);
        }

        public abstract void ConfigureTestServices(IServiceCollection services);
        public abstract void ConfigureTestContainer(ContainerBuilder builder);
    }

    public interface ITestHost
    {
        ITestOutputHelper OutputHelper { set; }
        HttpClient CreateClient();

    }
}
