using Autofac;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Serilog;
using ServiceComponents.Infrastructure.Senders;

namespace ReferenceApplication3.AspNet
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
            services.AddControllers(options =>
                    options.BuildMvcOptions() // -> Wireup (IMvcOptions)
            ).BuildMvc(); // -> Wireup (IMvcBuilder);

            services.BuildServices(); // -> Wireup

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "ReferenceApplication3.AspNet", Version = "v1" });
            });
        }


        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.AddCommandRouter(command => "_loopback");
            builder.AddQueryRouter(command => "_loopback");
            builder.AddEventRouter(command => "_loopback");
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ReferenceApplication3.AspNet v1"));
            }

            app.UseSerilogRequestLogging();

            app.UseRouting();

            app.UseAuthorization();

            app.BuildHealthCheckApp(); // -> Wireup (UseHealthCheck)

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.BuildEndpointRoot(Configuration); // -> Wireup (IEndpointRootBuilder)
            });
        }
    }
}
