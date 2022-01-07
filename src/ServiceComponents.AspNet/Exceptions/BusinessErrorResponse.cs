namespace ServiceComponents.AspNet.Exceptions
{
    public class BusinessErrorResponse : GenericErrorResponse
    {
        new public string Type => "business";

        public int ErrorCode { get; }

        public BusinessErrorResponse(int errorCode, string errorMessage) : base(errorMessage)
        {
            ErrorCode = errorCode;
        }
    }
}