namespace ServiceComponents.Api.Mediator
{
    public interface ICommand : IRequest
    {
        string CommandId { get; }
    }
}