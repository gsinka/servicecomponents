using System;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Monitoring;

namespace ServiceComponents.Infrastructure.Monitoring
{
    public abstract class RequestMetrics
    {
        protected readonly Type Type;

        [MetricField("namespace")]
        public string NameSpace { get; }

        [MetricField("name")]
        public string Name { get; }

        [MetricField("kind")]
        public string Kind { get; }
        
        private RequestMetrics(object obj)
        {
            Type = obj.GetType();

            NameSpace = Type.Namespace;
            Name = Type.Name;

            Kind = obj switch {
                ICommand command => "command",
                IQuery query => "query",
                IEvent evnt => "event",
                _ => "unknown"
            };
        }

        protected RequestMetrics(ICommand command) : this((object)command)
        { }

        public RequestMetrics(IQuery query) : this((object)query)
        { }

        public RequestMetrics(IEvent evnt) : this((object)evnt)
        { }
    }
}