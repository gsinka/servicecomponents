namespace ServiceComponents.AspNet.Exceptions
{
    public class SecurityErrorResponse : IErrorResponse
    {
        public string Type => "security";
        public string ErrorMessage { get; }

        public SecurityErrorResponse(string errorMessage)
        {
            ErrorMessage = errorMessage;
        }
    }
}