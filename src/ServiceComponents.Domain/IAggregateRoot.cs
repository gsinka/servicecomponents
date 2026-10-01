namespace ServiceComponents.Domain
{
    public interface IAggregateRoot
    {
        string AggregateId { get; }

        long Version { get; }
    }
}
