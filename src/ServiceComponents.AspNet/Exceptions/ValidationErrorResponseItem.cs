namespace ServiceComponents.AspNet.Exceptions
{
    public class ValidationErrorResponseItem
    {
        public string PropertyName { get; }
        public string ErrorCode { get; }
        public string ErrorMessage { get; }
        public object AttemptedValue { get; }

        public ValidationErrorResponseItem(string propertyName, string errorCode, string errorMessage, object attemptedValue)
        {
            PropertyName = propertyName;
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
            AttemptedValue = attemptedValue;
        }
    }
}