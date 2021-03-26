using System;
using System.Configuration;
using System.Drawing;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using NHibernate.Cfg.ConfigurationSchema;
using NHibernate.Tool.hbm2ddl;
using ReferenceApplication.Application;
using ReferenceApplication.AspNet.Wireup.Extensions;
using Serilog;
using ServiceComponents.AspNet;
using ServiceComponents.AspNet.Exceptions;
using ServiceComponents.AspNet.Http;
using ServiceComponents.AspNet.Monitoring;
using ServiceComponents.Infrastructure.NHibernate;

namespace ReferenceApplication.AspNet.Wireup
{
    public static class WireupExtensions
    {
        public static IHostBuilder WireupServiceComponents(this IHostBuilder hostBuilder, string[] args)
        {
            hostBuilder
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .UseSerilog((context, configuration) => configuration.ReadFrom.Configuration(context.Configuration));

            return hostBuilder;
        }

        public static IWebHostBuilder WireupServiceComponents(this IWebHostBuilder hostBuilder, string[] args)
        {
            return hostBuilder;
        }

        public static IServiceCollection WireupServiceComponents(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddControllers(options => {
                options.UseRequestBinder();
            })
                .AddApplicationPart(typeof(MetricsController).Assembly);


            services.AddHttpContextAccessor();
            services.AddHttpClient();
            services.Configure<HttpRequestOptions>(configuration.GetSection("httpRequest"));

            // Health check

            services.AddHealthChecks()
                .AddCheck("self", () => HealthCheckResult.Healthy(), new[] { HealthcheckExtensions.LivenessTag })
                .AddRabbitMQ(rabbitConnectionString: configuration.GetValue<string>("rabbitMQ:endpointUri"));

            // SSO

            services.ConfigureSingleSignOn(configuration);

            // OpenAPI

            services.ConfigureOpenApi(configuration);

            return services;
        }

        public static ContainerBuilder WireupServiceComponents(this ContainerBuilder builder, IConfiguration configuration)
        {
            builder.RegisterModule(new ServiceComponentsModule(configuration));

            builder.RegisterModule(new NhibernateModule(configuration.GetConnectionString("postgres"),
                mapping => mapping.FluentMappings.AddFromAssemblyOf<TestCommandHandler>(),
                configuration => new SchemaUpdate(configuration).Execute(true, true)));

            builder.RegisterModule(new RabbitModule(configuration));
            builder.RegisterModule<MonitoringModule>();

            return builder;
        }

        public static IApplicationBuilder WireupServiceComponents(this IApplicationBuilder app, IWebHostEnvironment env, IConfiguration configuration)
        {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            app.ConfigureOpenApi(configuration);

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.AddReadiness().AddLiveness();
            app
                .UseMiddleware<ErrorMetricsMiddleware>()
                .UseMiddleware<ErrorHandlingMiddleware>();

            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
            });
            return app;
        }
    }
}
