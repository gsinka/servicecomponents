using System.Collections.Generic;

namespace ServiceComponents.AspNet.Exceptions
{
    public class ValidationErrorResponse : GenericErrorResponse
    {
        new public string Type => "validation";

        public IEnumerable<ValidationErrorResponseItem> Errors { get; }

        public ValidationErrorResponse(string errorMessage, IEnumerable<ValidationErrorResponseItem> errors) 
            : base(errorMessage)
        {
            Errors = errors;
        }
    }
}