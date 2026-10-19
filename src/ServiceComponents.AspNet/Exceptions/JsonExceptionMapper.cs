using System;
using System.Collections.Generic;
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

            // DotNet.ErrorHandler exceptions (and their restored Core counterparts) are sent as ProblemDetails, so their metadata (Exception.Data) reaches the frontend too
            if (ErrorHandlerExceptionInfo.GetStatusCode(exception) is { } errorHandlerStatusCode) {
                return (errorHandlerStatusCode, ErrorHandlerExceptionInfo.CreateResponse(exception, errorHandlerStatusCode));
            }

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

            var body = httpContent.ReadAsStringAsync().Result;

            // Stays null if the body is not a known error response, then the status code decides the exception
            IErrorResponse errorResponse = null;
            try {
                var jsonObject = JObject.Parse(body);

                if (string.Equals(httpContent.Headers.ContentType?.MediaType, ErrorHandlerErrorResponse.ContentType, StringComparison.OrdinalIgnoreCase)) {
                    // ProblemDetails of a DotNet.ErrorHandler exception
                    errorResponse = ParseErrorHandlerResponse(statusCode, jsonObject);
                }
                else {
                    // A missing "type" property is not an error here, it just results in no known response
                    errorResponse = jsonObject.GetValue("type", StringComparison.OrdinalIgnoreCase)?.Value<string>() switch {

                        "business" => jsonObject.ToObject<BusinessErrorResponse>(),
                        "validation" => jsonObject.ToObject<ValidationErrorResponse>(),
                        "security" => jsonObject.ToObject<SecurityErrorResponse>(),
                        "generic" => jsonObject.ToObject<GenericErrorResponse>(),
                        _ => null
                    };
                }
            }
            catch (JsonException) {
                // not a known error body (e.g. proxy / gateway response), handled by the status code below
            }

            return (statusCode, errorResponse) switch {
                // The status of the ProblemDetails decides the exception
                (_, ErrorHandlerErrorResponse response) => CreateErrorHandlerException(response),
                (HttpStatusCode.Unauthorized, {} response) => new UnauthorizedAccessException(response.ErrorMessage),
                (HttpStatusCode.NotImplemented, {} response) => new NotImplementedException(response.ErrorMessage),
                (HttpStatusCode.NotFound, { } response) => new NotFoundException(response.ErrorMessage),
                // 409 and 410 have their own exceptions instead of falling to "Unknown error"
                (HttpStatusCode.Conflict, { } response) => new ConflictException(response.ErrorMessage),
                (HttpStatusCode.Gone, { } response) => new GoneException(response.ErrorMessage),
                (HttpStatusCode.BadRequest, BusinessErrorResponse response) => new BusinessException(response.ErrorCode, response.ErrorMessage),
                (HttpStatusCode.BadRequest, ValidationErrorResponse response) => new ValidationException(response.ErrorMessage, response.Errors.Select(error => new ValidationFailure(error.PropertyName, error.ErrorMessage, error.AttemptedValue) { ErrorCode = error.ErrorCode }), false),
                (HttpStatusCode.BadRequest, { } response) => new InvalidOperationException(response.ErrorMessage),
                // Unknown response: keep the status code and the body in the message to make the problem traceable
                (_, _) => new Exception(string.IsNullOrWhiteSpace(body) ? $"Unknown error ({(int)statusCode} {statusCode})" : $"Unknown error ({(int)statusCode} {statusCode}): {body}")
            };
        })
        {
        }

        /// <summary>
        /// Reads only what is needed to restore the exception: the status, the message and the original Exception.Data entries
        /// </summary>
        private static ErrorHandlerErrorResponse ParseErrorHandlerResponse(HttpStatusCode statusCode, JObject jsonObject)
        {
            return new ErrorHandlerErrorResponse {
                Status = jsonObject.Value<int?>("status") ?? (int)statusCode,
                Detail = jsonObject.Value<string>("detail") ?? jsonObject.Value<string>("title"),
                Data = jsonObject["data"]?.ToObject<Dictionary<string, JToken>>() ?? new Dictionary<string, JToken>()
            };
        }

        /// <summary>
        /// Creates the exception of a DotNet.ErrorHandler response with its metadata in Exception.Data
        /// </summary>
        private static Exception CreateErrorHandlerException(ErrorHandlerErrorResponse response)
        {
            Exception exception = (HttpStatusCode)response.Status switch {
                HttpStatusCode.NotFound => new NotFoundException(response.ErrorMessage),
                HttpStatusCode.Conflict => new ConflictException(response.ErrorMessage),
                HttpStatusCode.Gone => new GoneException(response.ErrorMessage),
                _ => new BusinessException(response.ErrorMessage)
            };

            // The error code of DotNet.ErrorHandler is a string, so it can't fit into BusinessException.ErrorCode, it stays in Data
            ErrorHandlerExceptionInfo.SetData(exception, response.Data);

            return exception;
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
            httpResponse.ContentType = response is ErrorHandlerErrorResponse ? ErrorHandlerErrorResponse.ContentType : MediaTypeNames.Application.Json;
            await httpResponse.WriteAsync(JsonConvert.SerializeObject(response));
        }

        public async Task ThrowExceptionIfNeeded(HttpResponseMessage httpResponse)
        {
            if (httpResponse.IsSuccessStatusCode) {
                return;
            }

            throw _exceptionMapper(httpResponse.StatusCode, httpResponse.Content) ?? new Exception("Unknown error");

        }
    }
}
