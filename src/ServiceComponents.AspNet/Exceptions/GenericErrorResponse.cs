using System;
using System.Net;
using System.Runtime.Serialization;
using FluentValidation;

namespace ServiceComponents.AspNet.Exceptions
{
    public class GenericErrorResponse : IErrorResponse
    {
        public string Type => "generic";
        public string ErrorMessage { get; }

        public GenericErrorResponse(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }
    }
}