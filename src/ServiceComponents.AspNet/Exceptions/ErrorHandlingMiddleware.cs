using System;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Serilog;
using ServiceComponents.Core.Exceptions;

namespace ServiceComponents.AspNet.Exceptions
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _request;
        private readonly ILogger _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger logger)
        {
            _request = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try {
                await _request.Invoke(context);
            }
            catch (ValidationException exception) {

                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = MediaTypeNames.Application.Json;
                await context.Response.WriteAsync(exception.Message);
            }
            catch (NotFoundException exception) {
                
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                context.Response.ContentType = MediaTypeNames.Text.Plain;
                await context.Response.WriteAsync(exception.Message);
            }
            catch (GoneException exception) {
                
                context.Response.StatusCode = StatusCodes.Status410Gone;
                context.Response.ContentType = MediaTypeNames.Text.Plain;
                await context.Response.WriteAsync(exception.Message);
            }
            catch (ConflictException exception) {
                
                context.Response.StatusCode = StatusCodes.Status409Conflict;
                context.Response.ContentType = MediaTypeNames.Text.Plain;
                await context.Response.WriteAsync(exception.Message);
            }
            catch (BusinessException exception) {

                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                context.Response.ContentType = MediaTypeNames.Text.Plain;
                await context.Response.WriteAsync(exception.Message);
            }
            catch (Exception exception) {

                context.Response.StatusCode = 500;
                context.Response.ContentType = MediaTypeNames.Text.Plain;
                await context.Response.WriteAsync(exception.Message);
            }
        }
    }
}
