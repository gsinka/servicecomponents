using System.Net;

namespace ServiceComponents.AspNet.Exceptions
{
    public class ErrorResponse
    {
        public HttpStatusCode StatusCode { get; }
        public int ErrorCode { get; }
        public string ErrorMessage { get; }

        public ErrorResponse(HttpStatusCode statusCode, int errorCode, string errorMessage)
        {
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
            StatusCode = statusCode;
        }
    }
}