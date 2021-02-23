namespace ServiceComponents.Application
{
    public interface ICorrelation
    {
        string CorrelationId { get; }
        string CausationId { get; }
        string CurrentId { get; }
    }
}