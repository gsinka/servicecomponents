namespace ServiceComponents.AspNet.Exceptions
{
    public interface IErrorResponse
    {
        string Type { get; }
        string ErrorMessage { get; }
    }
}