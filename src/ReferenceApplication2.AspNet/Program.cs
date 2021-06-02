using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;
using NHibernate.Tool.hbm2ddl;
using ReferenceApplication.Api;
using ReferenceApplication.Application;
using ReferenceApplication.Application.Entities;
using Serilog;
using Serilog.Events;
using ServiceComponents.AspNet.Exceptions;
using ServiceComponents.AspNet.Monitoring;
using ServiceComponents.AspNet.Wireup;
using ServiceComponents.Infrastructure.Behaviors.CommandConstraints;
using Swashbuckle.AspNetCore.Filters;

namespace ReferenceApplication2.AspNet
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            Log.Information("Starting service");
            host.Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            return new ServiceComponentsHostBuilder()
                
                .UseDefault(

                    new[] { typeof(TestCommand).Assembly },
                    new[] { typeof(TestCommandHandler).Assembly })

                .ConfigureApp((configuration, environment, app) => {

                    if (environment.IsDevelopment()) {
                        app.UseDeveloperExceptionPage();
                    }

                    app.UseMiddleware<ErrorHandlingMiddleware>();

                    app.UseCors(builder => {
                        builder.AllowAnyOrigin();
                        builder.AllowAnyHeader();
                        builder.AllowAnyMethod();
                    });

                    app.UseRouting();

                    app.UseAuthentication();
                    app.UseAuthorization();
                })

                // Add endpoints
                .AddEndpoints()

                // Use serilog for logging
                .UseSerilog((context, log) => log
                    .WriteTo.Console(LogEventLevel.Information)
                    .WriteTo.Seq("http://localhost:5341", LogEventLevel.Verbose)
                    .Enrich.FromLogContext()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("NHibernate", LogEventLevel.Warning)
                    .MinimumLevel.Verbose())

                // Add OpenApi
                .AddOpenApi(
                    (configuration, options) => {
                        options.SwaggerDoc("v1", new OpenApiInfo {
                            Title = "Reference Application", 
                            Version = "v1"
                        });

                        var appXmlDoc = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
                        options.IncludeXmlComments(appXmlDoc);

                        var aspNetXmlDoc = Path.Combine(AppContext.BaseDirectory, $"{typeof(MetricsController).Assembly.GetName().Name}.xml");
                        options.IncludeXmlComments(aspNetXmlDoc);

                        options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme() {
                            Type = SecuritySchemeType.OpenIdConnect,
                            OpenIdConnectUrl = new Uri("http://localhost:8080/auth/realms/develop/.well-known/openid-configuration"),
                            In = ParameterLocation.Header,
                            BearerFormat = "JWT",
                            Scheme = JwtBearerDefaults.AuthenticationScheme
                        });

                        options.OperationFilter<SecurityRequirementsOperationFilter>(true, JwtBearerDefaults.AuthenticationScheme);

                    },
                    (configuration, options) => {
                        
                        options.OAuthClientId("reference-app");
                        options.OAuthScopes("openid", "profile");

                        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Reference Application v1");
                    })

                .RegisterCallback((configuration, services) => services.AddSwaggerGenNewtonsoftSupport())

                // Health check
                .AddHealthCheck((configuration, check) => {
                    check.AddRabbitMQ(rabbitConnectionString: "amqp://localhost:5672");
                    check.AddRedis("localhost");
                })

                // Add http sender
                .AddHttpSender(new Uri("http://localhost:5000/api/generic"), "http")

                // Routing
                .AddCommandRouter(command => "loopback")
                .AddQueryRouter(query => "loopback")
                .AddEventRouter(evnt => "loopback")

                // Redis
                //.AddRedis(configuration => "localhost:6379")

                // Rabbit
                //.AddRabbit("amqp://guest:guest@localhost:5672", "test2", "test-queue", "test-exchange", retryIntervals: new[] { 1000, 3000, 5000 })
                .AddRabbit("amqp://guest:guest@localhost:5672", "test2", "test-queue", "test-exchange")

                .ConfigureMvc(builder => builder.AddNewtonsoftJson(options => {
                    options.UseCamelCasing(true);
                    options.SerializerSettings.Converters.Add(new StringEnumConverter());
                }))

                .AddRedisDistributedCache("localhost")

                .ConfigureContainer((context, builder) => {

                    //builder.AddRequestConstraints(request => request switch {

                    //    LongCommand longCommand => new[] { "test1", "test2" },
                    //    TestCommand testCommand => new[] { "test2" },
                    //    _ => default

                    //}, (key, count) => (key, count) switch {

                    //    ("test1", _) when count > 0 => false,
                    //    ("test2", _) when count > 0 => false,
                    //    (_, _) => true

                    //}, (key) => TimeSpan.FromSeconds(30));
                })

                // NHibernate
                .AddNHibernate(
                    configuration => "Server=localhost; Port=5432; Database=ref-app; User Id=postgres; Password=postgres",
                    map => map.FluentMappings.AddFromAssemblyOf<TestEntity>(),
                    configuration => new SchemaUpdate(configuration).Execute(true, true))

                .AddPrometheusMetrics()

                .AddBadge()

                .CreateHostBuilder(args);
        }
    }
}
