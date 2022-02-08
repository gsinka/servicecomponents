using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ServiceComponents.Core.Exceptions;

namespace ServiceComponents.AspNet.Exceptions
{
    public class JsonExceptionMapper : IExceptionMapperService
    {
        private readonly Func<Exception, (int statusCode, IErrorResponse response)> _responseMapper;
        private readonly Func<HttpStatusCode, HttpContent, Exception> _exceptionMapper;

        /// <summary>
        /// Exception mapper with built-in mapping
        /// </summary>
        public JsonExceptionMapper() : this (exception => {

            return exception switch {
                UnauthorizedAccessException unauthorizedAccessException => ((int)HttpStatusCode.Unauthorized, new SecurityErrorResponse(unauthorizedAccessException.Message)),
                NotImplementedException notImplementedException => ((int)HttpStatusCode.NotImplemented, new GenericErrorResponse(notImplementedException.Message)),
                NotFoundException notFoundException => new((int)HttpStatusCode.NotFound, new GenericErrorResponse(notFoundException.Message)),
                BusinessException businessException => new((int)HttpStatusCode.BadRequest, new BusinessErrorResponse(businessException.ErrorCode, businessException.Message)),
                ValidationException validationException => new((int)HttpStatusCode.BadRequest, new ValidationErrorResponse("Validation failed", validationException.Errors.Select(failure => new ValidationErrorResponseItem(failure.PropertyName, failure.ErrorCode, failure.ErrorMessage, failure.AttemptedValue)))),
                InvalidOperationException invalidOperationException => new((int)HttpStatusCode.BadRequest, new GenericErrorResponse(exception.Message)),
                _ => new((int)HttpStatusCode.InternalServerError, new GenericErrorResponse("Something really bad happened"))
            };

        }, (statusCode, httpContent) => {

            var jsonObject = JObject.Parse(httpContent.ReadAsStringAsync().Result);

            var errorResponse = jsonObject.GetValue("type", StringComparison.OrdinalIgnoreCase)!.Value<string>() switch {
                
                "business" => jsonObject.ToObject<BusinessErrorResponse>(),
                "validation" => jsonObject.ToObject<ValidationErrorResponse>(),
                "security" => jsonObject.ToObject<SecurityErrorResponse>(),
                "generic" => jsonObject.ToObject<GenericErrorResponse>(),
                _ => null
            };

            return (statusCode, errorResponse) switch {
                (HttpStatusCode.Unauthorized, {} response) => new UnauthorizedAccessException(response.ErrorMessage),
                (HttpStatusCode.NotImplemented, {} response) => new NotImplementedException(response.ErrorMessage),
                (HttpStatusCode.NotFound, { } response) => new NotFoundException(response.ErrorMessage),
                (HttpStatusCode.BadRequest, BusinessErrorResponse response) => new BusinessException(response.ErrorCode, response.ErrorMessage),
                (HttpStatusCode.BadRequest, ValidationErrorResponse response) => new ValidationException(response.ErrorMessage, response.Errors.Select(error => new ValidationFailure(error.PropertyName, error.ErrorMessage, error.AttemptedValue) { ErrorCode = error.ErrorCode }), false),
                (HttpStatusCode.BadRequest, { } response) => new InvalidOperationException(response.ErrorMessage),
                (_, _) => new Exception("Unknown error")
            };
        })
        {
        }

        /// <summary>
        /// Exception mapper with custom mapping
        /// </summary>
        /// <param name="responseMapper"></param>
        /// <param name="exceptionMapper"></param>
        public JsonExceptionMapper(Func<Exception, (int statusCode, IErrorResponse response)> responseMapper, Func<HttpStatusCode, HttpContent, Exception> exceptionMapper)
        {
            _responseMapper = responseMapper;
            _exceptionMapper = exceptionMapper;
        }

        public async Task WriteResponse(Exception exception, HttpResponse httpResponse)
        {
            var (statusCode, response) = _responseMapper(exception);

            httpResponse.StatusCode = statusCode;
            httpResponse.ContentType = MediaTypeNames.Application.Json;
            await httpResponse.WriteAsync(JsonConvert.SerializeObject(response));
        }

        public async Task ThrowExceptionIfNeeded(HttpResponseMessage httpResponse)
        {
            if (httpResponse.IsSuccessStatusCode) return;
            throw _exceptionMapper(httpResponse.StatusCode, httpResponse.Content) ?? new Exception("Unknown error");

        }
    }
}
