namespace ServiceComponents.Api.Mediator
{
    public interface IQuery
    {
        string QueryId { get; }
    }

    public interface IQuery<TResult> : IQuery
    {
    }
}