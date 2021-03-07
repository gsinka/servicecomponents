using ServiceComponents.Infrastructure.CorrelationContext;

namespace ServiceComponents.Infrastructure.Receivers
{
    public abstract class LoopbackReceiverCorrelationBehavior
    {
        private readonly Correlation _correlation;
        private string _causationId;
        private string _currentId;

        protected LoopbackReceiverCorrelationBehavior(Correlation correlation)
        {
            _correlation = correlation;
        }

        protected void SetCorrelation(string requestId)
        {
            _causationId = _correlation.CausationId;
            _currentId = _correlation.CurrentId;

            _correlation.CausationId = _correlation.CurrentId;
            _correlation.CurrentId = requestId;
        }

        protected void ResetCorrelation()
        {
            _correlation.CurrentId = _currentId;
            _correlation.CausationId = _causationId;
        }
    }
}