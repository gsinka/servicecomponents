using System;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using NHibernate.Tool.hbm2ddl;
using ReferenceApplication.Api;
using ReferenceApplication.Application;
using ReferenceApplication.Application.Entities;
using Serilog;
using Serilog.Events;
using ServiceComponents.AspNet.Wireup;
using ServiceComponents.Infrastructure.Monitoring;

namespace ReferenceApplication2.AspNet
{
    public class Program
    {
        public static void Main(string[] args)
        {
            new ServiceComponentsHostBuilder()
                .UseDefault(
                
                    new [] { typeof(TestCommand).Assembly },
                    new []{ typeof(TestCommandHandler).Assembly})

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
                    (configuration, options) => options.SwaggerDoc("v1", new OpenApiInfo { Title = "ReferenceApplication2.AspNet", Version = "v1" }), 
                    (configuration, options) => options.SwaggerEndpoint("/swagger/v1/swagger.json", "ReferenceApplication2.AspNet v1"))

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
                .AddRedis(configuration => "localhost:6379")
                .AddRedisCommandRules((command, commands) => commands.All(x => x.GetType() != command.GetType()))

                // Rabbit
                //.AddRabbit("amqp://guest:guest@localhost:5672", "test2", "test-queue", "test-exchange", retryIntervals: new[] { 1000, 3000, 5000 })
                .AddRabbit("amqp://guest:guest@localhost:5672", "test2", "test-queue", "test-exchange")

                .RegisterCallback(builder => builder.AddNewtonsoftJson(options => options.UseCamelCasing(true)))

                // NHibernate
                .AddNHibernate(
                    configuration => "Server=localhost; Port=5432; Database=ref-app; User Id=postgres; Password=postgres", 
                    map => map.FluentMappings.AddFromAssemblyOf<TestEntity>(),
                    configuration => new SchemaUpdate(configuration).Execute(true, true))
                
                //.RegisterCallback((context, builder) => builder.AddPrometheusRequestMetricsBehavior())

                .Build(args).Run();
        }
    }
}
