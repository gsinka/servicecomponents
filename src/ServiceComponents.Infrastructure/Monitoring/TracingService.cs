using OpenTracing;
using ServiceComponents.Application.Monitoring;

namespace ServiceComponents.Infrastructure.Monitoring
{
    public class TracingService : ITracingService
    {
        private readonly ITracer _tracer;

        public TracingService(ITracer tracer)
        {
            _tracer = tracer;
        }
    }
}
