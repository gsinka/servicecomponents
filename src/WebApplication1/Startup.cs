using System.Linq;
using Autofac;
using AutofacSerilogIntegration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json.Converters;
using Serilog;
using ServiceComponents.Application.Monitoring;
using ServiceComponents.AspNet;
using ServiceComponents.AspNet.Badge;
using ServiceComponents.AspNet.Exceptions;
using ServiceComponents.AspNet.Http;
using ServiceComponents.AspNet.Http.Senders;
using ServiceComponents.AspNet.Monitoring;
using ServiceComponents.AspNet.OpenApi;
using ServiceComponents.Core.Services;
using ServiceComponents.Infrastructure.Behaviors.Logging;
using ServiceComponents.Infrastructure.Behaviors.Stopwatch;
using ServiceComponents.Infrastructure.CorrelationContext;
using ServiceComponents.Infrastructure.Mediator;
using ServiceComponents.Infrastructure.Monitoring;
using ServiceComponents.Infrastructure.Rabbit;
using ServiceComponents.Infrastructure.Receivers;
using ServiceComponents.Infrastructure.Receivers.Loopback;
using ServiceComponents.Infrastructure.Validation;

namespace WebApplication1
{
    public class Startup
    {
        private readonly CustomStartup _customStartup;

        public Startup(IConfiguration configuration, IHostEnvironment hostEnvironment)
        {
            Configuration = configuration;
            _customStartup = new CustomStartup(configuration);
            HostEnvironment = hostEnvironment;
        }

        public IConfiguration Configuration { get; }
        public IHostEnvironment HostEnvironment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers(options => {

                // Request binder to handle commands, queries and events in controller actions
                options.ModelBinderProviders.Insert(0, new RequestBinderProvider());

                // Hide specific controllers (like metrics)
                options.Conventions.Add(new ActionHidingConvention(_customStartup.ActionsToHide));
            })
                // Newtonsoft
                .AddNewtonsoftJson(options => {
                    options.UseCamelCasing(true);
                    options.SerializerSettings.Converters.Add(new StringEnumConverter());
                })

                // Badge
                .PartManager.ApplicationParts.Add(new AssemblyPart(typeof(BadgeController).Assembly));

            services.AddHttpContextAccessor();
            services.AddHttpClient();
            services.AddCors();

            var healthCheckBuilder = services.AddHealthChecks();
            healthCheckBuilder.AddCheck("self", () => HealthCheckResult.Healthy(), new[] { HealthCheckConstants.LivenessTag });

            // Do project specific health check registration
            _customStartup.ConfigureHealthCheck(healthCheckBuilder);

            services.AddSwaggerGenNewtonsoftSupport();

            // Badge

            services.AddSingleton(provider => {
                var badgeService = new BadgeService(provider.GetRequiredService<HealthCheckService>());
                badgeService.RegistrateVersionBadge();
                badgeService.RegistrateLivenessBadge();
                badgeService.RegistrateReadinessBadge();
                return badgeService;
            });

            services.AddSingleton<IMetricsService, PrometheusMetricsService>();

            // Authentication
            services.AddAuthorization();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options => {
                    Configuration.GetSection("Authentication").Bind(options);

                    options.Audience ??= "account";
                    options.RequireHttpsMetadata = !HostEnvironment.IsDevelopment();
                    options.TokenValidationParameters.ValidIssuer ??= options.Authority;
                });

            // Do project specific service configuration
            _customStartup.ConfigureServices(services);
        }

        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterLogger();

            builder.RegisterType<ComputerClock>().AsImplementedInterfaces().SingleInstance();

            builder.AddHttpRequestParser();
            builder.AddMediator(_customStartup.ApplicationAssembly);
            builder.AddValidationBehavior(_customStartup.ApiAssembly);
            builder.AddLogBehavior();
            builder.AddStopwatchBehavior();

            // Correlation
            builder.AddCorrelationInfo();
            builder.AddReceiverCorrelationLogEnricherBehavior();
            builder.AddHttpReceiverCorrelationBehavior();
            builder.AddHttpSenderCorrelationBehavior();
            builder.AddLoopbackReceiverCorrelationBehavior();
            builder.AddRabbitReceiverCorrelationBehavior();
            builder.AddRabbitSenderCorrelationBehavior();

            // Receivers
            builder.AddReceivers();
            builder.AddHttpReceivers();
            builder.AddLoopbackReceivers();

            const string loopBack = "_loopback";

            // Senders
            builder.AddLoopbackCommandSender(loopBack);
            builder.AddLoopbackQuerySender(loopBack);
            builder.AddLoopbackEventPublisher(loopBack);

            // Metrics
            builder.AddPrometheusRequestMetricsService();
            builder.AddPrometheusRequestMetricsBehavior();

            _customStartup.ConfigureContainer(builder);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment()) {
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

            app.UseHealthChecks(HealthCheckConstants.ReadinessPath, new HealthCheckOptions {
                Predicate = registration =>
                    registration.Tags == null || !registration.Tags.Any() ||
                    registration.Tags.Contains(HealthCheckConstants.ReadinessTag)
            });

            app.UseHealthChecks(HealthCheckConstants.LivenessPath, new HealthCheckOptions {
                Predicate = registration =>
                    registration.Tags == null || !registration.Tags.Any() ||
                    registration.Tags.Contains(HealthCheckConstants.LivenessTag)
            });

            _customStartup.Configure(app, env);

            app.UseEndpoints(endpoints => {

                endpoints.MapControllers();

                // Badge
                const string badgePath = ".badge"; //TODO: move to options?
                endpoints.MapControllerRoute(name: "badge", pattern: $"{badgePath.TrimEnd('/')}/{{name?}}", defaults: new { controller = "Badge", action = "Get" });
            });
        }
    }
}
