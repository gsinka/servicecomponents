using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Autofac;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using ReferenceApplication.Api;
using ReferenceApplication.Application;
using Serilog;
using Serilog.Events;
using ServiceComponents.AspNet.Http.Senders;
using ServiceComponents.Core.Extensions;
using ServiceComponents.Infrastructure.Senders;
using Swashbuckle.AspNetCore.Filters;


namespace WebApplication1
{
    public class CustomStartup
    {
        public IConfiguration Configuration { get; }
        
        public CustomStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public static void ConfigureSerilog(HostBuilderContext context, IServiceProvider provider, LoggerConfiguration config)
        {
            config
                .MinimumLevel.Verbose()
                .WriteTo.Console(LogEventLevel.Information)
                .WriteTo.Seq("http://localhost:5341", LogEventLevel.Verbose)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                ;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSwaggerGen(c => {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "WebApplication1", Version = "v1" });
                var appXmlDoc = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
                if (File.Exists(appXmlDoc)) { c.IncludeXmlComments(appXmlDoc); }

                c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme() {
                    Type = SecuritySchemeType.OpenIdConnect,
                    OpenIdConnectUrl = new Uri("http://localhost:8080/auth/realms/develop/.well-known/openid-configuration"),
                    In = ParameterLocation.Header,
                    BearerFormat = "JWT",
                    Scheme = JwtBearerDefaults.AuthenticationScheme
                });
                c.OperationFilter<SecurityRequirementsOperationFilter>(true, JwtBearerDefaults.AuthenticationScheme);
            });


        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            //builder.AddHttpCommandSender(new Uri("http://localhost:5000/api/generic"), "http");
            //builder.AddHttpQuerySender(new Uri("http://localhost:5000/api/generic"), "http");
            //builder.AddHttpEventPublisher(new Uri("http://localhost:5000/api/generic"), "http");
            
            builder.AddCommandRouter(command => "_loopback");
            builder.AddQueryRouter(command => "_loopback");
            builder.AddEventRouter(command => "_loopback");
            
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseSwagger(options => { });
            app.UseSwaggerUI(options => {
                options.OAuthClientId("ref-app");
                options.OAuthScopes("openid", "profile");
                options.SwaggerEndpoint("/swagger/v1/swagger.json", $"ref-app v{Assembly.GetExecutingAssembly().ProductVersion()}");
            });
        }

        public void ConfigureHealthCheck(IHealthChecksBuilder builder)
        {
        }
        

        public List<Func<ActionModel, bool>> ActionsToHide => new() {
            model => model.Controller.ControllerName.Equals("Metrics", StringComparison.InvariantCultureIgnoreCase)
        };

        public Assembly ApiAssembly => typeof(TestCommand).Assembly;

        public Assembly ApplicationAssembly => typeof(TestCommandHandler).Assembly;
    }
}
