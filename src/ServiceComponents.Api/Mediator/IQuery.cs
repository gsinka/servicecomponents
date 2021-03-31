namespace ServiceComponents.Api.Mediator
{
    public interface IQuery : IRequest
    {
        string QueryId { get; }
    }

    public interface IQuery<TResult> : IQuery
    {
    }
}