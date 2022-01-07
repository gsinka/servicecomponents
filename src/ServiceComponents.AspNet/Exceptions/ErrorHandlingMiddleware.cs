using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace ServiceComponents.AspNet.Exceptions
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _request;
        private readonly ILogger _log;
        private readonly IExceptionMapperService _exceptionMapperService;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger log, IExceptionMapperService exceptionMapperService)
        {
            _request = next;
            _log = log;
            _exceptionMapperService = exceptionMapperService;
        }

        public async Task Invoke(HttpContext context)
        {
            try {
                await _request.Invoke(context);
            }
            catch (Exception exception) {
                await _exceptionMapperService.WriteResponse(exception, context.Response);
            }            
        }
    }
}
