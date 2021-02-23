using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Autofac;
using ReferenceApplication.AspNet.Wireup;
using ServiceComponents.AspNet;
using ServiceComponents.AspNet.OpenApi;
using ServiceComponents.Infrastructure.Http;

namespace ReferenceApplication.AspNet
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers(options => {
                
                // Use JSON request binder
                options.UseRequestBinder();
            });

            services.AddHttpContextAccessor();
            services.AddHttpClient();
            services.Configure<HttpRequestOptions>(Configuration.GetSection("httpRequest"));
            
            // SSO

            services.ConfigureSingleSignOn(Configuration);

            // OpenAPI

            services.ConfigureOpenApi(Configuration);
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterModule<ServiceComponentsModule>();
            builder.RegisterModule<RabbitModule>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.ConfigureOpenApi(Configuration);

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
