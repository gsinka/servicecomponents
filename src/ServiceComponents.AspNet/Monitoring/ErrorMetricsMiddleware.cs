using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ServiceComponents.AspNet.Monitoring
{
    public class ErrorMetricsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly MetricsService _metricsService;

        public ErrorMetricsMiddleware(RequestDelegate next, MetricsService metricsService)
        {
            _next = next;
            _metricsService = metricsService;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next.Invoke(context);
            }
            catch (Exception e)
            {
                _metricsService.AddException(e);
                throw;
            }

        }
    }
}
