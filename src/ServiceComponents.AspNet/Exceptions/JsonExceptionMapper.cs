using System;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace ServiceComponents.AspNet.Exceptions
{
    public class JsonExceptionMapper : IExceptionMapperService
    {
        private readonly Func<Exception, ErrorResponse> _responseMapper;
        private readonly Func<ErrorResponse, Exception> _exceptionMapper;

        public JsonExceptionMapper(Func<Exception, ErrorResponse> responseMapper, Func<ErrorResponse, Exception> exceptionMapper)
        {
            _responseMapper = responseMapper;
            _exceptionMapper = exceptionMapper;
        }

        public async Task WriteResponse(Exception exception, HttpResponse httpResponse)
        {
            var response = _responseMapper(exception);

            httpResponse.StatusCode = (int)response.StatusCode;
            httpResponse.ContentType = MediaTypeNames.Application.Json;
            await httpResponse.WriteAsync(JsonConvert.SerializeObject(new { errorCode = response.ErrorCode, errorMessage = response.ErrorMessage }));
        }

        public async Task ThrowExceptionIfNeeded(HttpResponseMessage httpResponse)
        {
            var errorResponse = JsonConvert.DeserializeAnonymousType(await httpResponse.Content.ReadAsStringAsync(), new { errorCode = 1, errorMessage = ""});
            var exception = _exceptionMapper(new ErrorResponse(httpResponse.StatusCode, errorResponse.errorCode, errorResponse.errorMessage));
            
            if (exception != null) {
                throw exception;
            }
        }
    }
}
