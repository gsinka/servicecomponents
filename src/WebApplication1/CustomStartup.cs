using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Autofac;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using RabbitMQ.Client;
using ReferenceApplication.Api;
using ReferenceApplication.Application;
using Serilog;
using Serilog.Events;
using ServiceComponents.Core.Extensions;
using ServiceComponents.Infrastructure.Options;
using ServiceComponents.Infrastructure.Rabbit;
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
                .WriteTo.Console(LogEventLevel.Information, outputTemplate: "[{Timestamp:HH:mm:ss+fff}{EventType:x8} {Level:u3}] {Message:lj} [{SourceContext}]{NewLine}{Exception}")
                .WriteTo.Seq("http://localhost:5341", LogEventLevel.Verbose)
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .MinimumLevel.Override("NHibernate", LogEventLevel.Verbose)
                .MinimumLevel.Override("NHibernate.SQL", LogEventLevel.Verbose)
                .Enrich.FromLogContext()
                ;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSwaggerGen(c => {
                c.SwaggerDoc("v1", new OpenApiInfo {
                    Title = Configuration.GetValue<string>("Application:Name"),
                    Version = $"v{ApplicationOptions.InformationalVersion}",
                    Description = Configuration.GetValue<string>("Application:Description")
                });

                var appXmlDoc = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");
                if (File.Exists(appXmlDoc)) { c.IncludeXmlComments(appXmlDoc); }

                var authority = Configuration.GetValue<string>("Swagger:Authentication:Authority") ?? Configuration.GetValue<string>("Authentication:Authority")?.TrimEnd('/');

                if (!string.IsNullOrWhiteSpace(authority)) {
                    c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme() {
                        Type = SecuritySchemeType.OpenIdConnect,
                        OpenIdConnectUrl = new Uri($"{authority}/.well-known/openid-configuration"),
                        In = ParameterLocation.Header,
                        BearerFormat = "JWT",
                        Scheme = JwtBearerDefaults.AuthenticationScheme
                    });
                    c.OperationFilter<SecurityRequirementsOperationFilter>(true, JwtBearerDefaults.AuthenticationScheme);
                }
            });

        }

        public void ConfigureContainer(ContainerBuilder builder)
    {
        //builder.AddHttpCommandSender(new Uri("http://localhost:5000/api/generic"), "http");
        //builder.AddHttpQuerySender(new Uri("http://localhost:5000/api/generic"), "http");
        //builder.AddHttpEventPublisher(new Uri("http://localhost:5000/api/generic"), "http");

        builder.AddCommandRouter(command => "_loopback");
        builder.AddQueryRouter(command => "_loopback");
        builder.AddEventRouter(command => "rabbit");


        var rabbitClientName = "ref-app";
        var exchange = "ref-app";
        var queue = "ref-app";
        var rabbitUri = new Uri("amqp://guest:guest@localhost:5672");
        var routingKey = "";


        // Publisher connection, channel

        builder.AddRabbitConnection(rabbitUri, $"{rabbitClientName}-publisher", "publisher");
        builder.AddRabbitChannel(key: "publisher", connectionKey: "publisher");
        builder.AddRabbitEventPublisher(exchange, routingKey, channelKey: "publisher", key: "rabbit");

        // Consumers

        builder.AddRabbitConnection(rabbitUri, $"{rabbitClientName}-consumer", "consumer");
        builder.AddRabbitChannel(connectionKey: "consumer", key: $"consumer");
        builder.AddRabbitConsumer(queue, $"{rabbitClientName}-consumer", $"consumer", $"consumer");

        builder.AddRabbitReceivers();
        builder.AddRabbitCommandSender(exchange, string.Empty, "publisher", "rabbit");
        builder.AddRabbitQuerySender(exchange, string.Empty, "publisher", "rabbit");

        // NHibernate
        //builder.RegisterModule(new NhibernateModule(
        //        "Server=localhost; Port=5432; Database=ref-app; User Id=postgres; Password=postgres",
        //        map => map.FluentMappings.AddFromAssemblyOf<TestEntity>(),
        //        configuration => new SchemaUpdate(configuration).Execute(true, true)));
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseSerilogRequestLogging();
        app.UseSwagger(options => { });
        app.UseSwaggerUI(options => {
            options.OAuthClientId(Configuration.GetValue<string>("Swagger:Authentication:Client"));
            options.OAuthScopes("openid", "profile");
            options.SwaggerEndpoint("/swagger/v1/swagger.json", $"{Configuration.GetValue<string>("Application:ShortName")} v{ApplicationOptions.ApiVersion}");
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

    public static void Initialize(IHost host)
    {
        var scope = host.Services.GetRequiredService<ILifetimeScope>();

        // Rabbit init

        var channel = scope.ResolveKeyed<IModel>("publisher");
        channel.ExchangeDeclare("ref-app", "direct", false, true);
        channel.QueueDeclare("ref-app", false, false, true);
        channel.QueueBind("ref-app", "ref-app", "");

        var consumer = scope.ResolveKeyed<RabbitConsumer>("consumer");
        Log.Verbose("Starting consumer consumer-{consumerId}", consumer.ConsumerTag);
        consumer.StartAsync(CancellationToken.None).Wait();
    }
}
}
