namespace ServiceComponents.AspNet.Exceptions
{
    public class SecurityErrorResponse : GenericErrorResponse
    {
        new public string Type => "security";

        public SecurityErrorResponse(string errorMessage) : base(errorMessage) { }
    }
}