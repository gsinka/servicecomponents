using System;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Infrastructure.Monitoring
{
    [MetricDescription("request_failure_counter", "Request failure counter")]
    public class RequestFailureMetric : RequestMetrics
    {
        [MetricField("exception_name")]
        public string ExceptionName { get; }
        
        public RequestFailureMetric(ICommand command, Exception exception) : base(command)
        {
            ExceptionName = exception.GetType().Name;
        }
        
        public RequestFailureMetric(IQuery query, Exception exception) : base(query)
        {
            ExceptionName = exception.GetType().Name;
        }
        
        public RequestFailureMetric(IEvent evnt, Exception exception) : base(evnt)
        {
            ExceptionName = exception.GetType().Name;
        }
    }
}