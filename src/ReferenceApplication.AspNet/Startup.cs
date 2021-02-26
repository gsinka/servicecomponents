using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Autofac;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NHibernate;
using NHibernate.Tool.hbm2ddl;
using ReferenceApplication.AspNet.Wireup;
using ReferenceApplication.AspNet.Wireup.Extensions;
using ServiceComponents.AspNet;
using ServiceComponents.AspNet.Metrics;
using ServiceComponents.AspNet.OpenApi;
using ServiceComponents.Infrastructure.Http;
using ServiceComponents.Infrastructure.NHibernate;

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
            })
                // Metrics
                .AddApplicationPart(typeof(MetricsController).Assembly);

            services.AddHttpContextAccessor();
            services.AddHttpClient();
            services.Configure<HttpRequestOptions>(Configuration.GetSection("httpRequest"));
            
            // Health check

            services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy(), new[] {HealthcheckExtensions.LivenessTag})
                .AddRabbitMQ(rabbitConnectionString: Configuration.GetValue<string>("rabbitMQ:endpointUri"));

            // SSO

            services.ConfigureSingleSignOn(Configuration);

            // OpenAPI

            services.ConfigureOpenApi(Configuration);
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterModule<ServiceComponentsModule>();

            builder.RegisterModule(new NhibernateModule(
                Configuration.GetConnectionString("postgres"), 
                mapping => mapping.FluentMappings.AddFromAssemblyOf<OutgoingEventEntity>(), 
                configuration => new SchemaUpdate(configuration).Execute(true, true)));

            builder.RegisterModule(new RabbitModule(Configuration));

            builder.AddNhibernateRabbitPublisher();
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
            app.AddReadiness().AddLiveness();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
