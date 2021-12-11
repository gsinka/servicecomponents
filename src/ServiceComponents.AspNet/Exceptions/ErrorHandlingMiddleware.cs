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
            //catch (ValidationException exception) {

            //    _log.Error(exception, "Validation failed");
            //    context.Response.StatusCode = StatusCodes.Status400BadRequest;
            //    context.Response.ContentType = MediaTypeNames.Application.Json;
            //    await context.Response.WriteAsync(exception.Message);
            //}
            //catch (NotFoundException exception) {

            //    _log.Error(exception, "The requested resource not found");
            //    context.Response.StatusCode = StatusCodes.Status404NotFound;
            //    context.Response.ContentType = MediaTypeNames.Text.Plain;
            //    await context.Response.WriteAsync(exception.Message);
            //}
            //catch (GoneException exception) {

            //    _log.Error(exception, "The requested resource does not exist anymore");
            //    context.Response.StatusCode = StatusCodes.Status410Gone;
            //    context.Response.ContentType = MediaTypeNames.Text.Plain;
            //    await context.Response.WriteAsync(exception.Message);
            //}
            //catch (ConflictException exception) {

            //    _log.Error(exception, "The request caused a conflict");
            //    context.Response.StatusCode = StatusCodes.Status409Conflict;
            //    context.Response.ContentType = MediaTypeNames.Text.Plain;
            //    await context.Response.WriteAsync(exception.Message);
            //}
            //catch (BusinessException exception) {

            //    _log.Error(exception, "Business failure");
            //    context.Response.StatusCode = StatusCodes.Status400BadRequest;
            //    context.Response.ContentType = MediaTypeNames.Text.Plain;
            //    await context.Response.WriteAsync(exception.Message);
            //}
            //catch (Exception exception) {

            //    _log.Error(exception, "Something really bad happened");
            //    context.Response.StatusCode = 500;
            //    context.Response.ContentType = MediaTypeNames.Text.Plain;
            //    await context.Response.WriteAsync(exception.Message);
            //}
        }
    }
}
