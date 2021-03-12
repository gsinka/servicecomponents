using System;
using System.Diagnostics;
using System.Linq;
using Prometheus;

/*
 Not using quantiles: https://www.robustperception.io/how-does-a-prometheus-summary-work
 */

namespace ServiceComponents.AspNet.Metrics
{
    public class MetricsService
    {
        private Summary _summary;
        private Summary HandlerDurations
        {
            get
            {
                if (_summary == null)
                    _summary = Prometheus.Metrics.CreateSummary("microservice_handler_durations", "Auto-generated summary for handler durations in second", new SummaryConfiguration() { LabelNames = new[] { "handler_namespace", "handler_name","trigger_namespace", "trigger_name", "handler_type" } });
                return _summary;
            }
        }

        private Counter _counter;

        private Counter Counter
        {
            get
            {
                if (_counter == null) _counter = Prometheus.Metrics.CreateCounter("microservice_counters", "Auto-generated counter metrics", new CounterConfiguration() { LabelNames = new[] { "namespace", "name", "metric_type" } });
                return _counter;
            }
        }
        private Counter _exceptionCounter;
        private Counter ExceptionCounter
        {
            get
            {
                if (_exceptionCounter == null) _exceptionCounter = Prometheus.Metrics.CreateCounter("microservice_exceptions", "Auto-generated exception counter metrics", new CounterConfiguration() { LabelNames = new[] { "file", "line", "exception_namespace", "exception_name" } });
                return _exceptionCounter;
            }
        }
        public void PutObservationInSecond(string handlerNamespace, string handlerName, string triggerNamespace,string triggerName, double val, string type = "")
        {
            HandlerDurations.
                WithLabels(
                handlerNamespace,
                handlerName,
                triggerNamespace,
                triggerName,
                type).
                Observe(val);
        }

        public void IncrementCounter<T>(double increment = 1, string type = "")
        {
            Counter.
                WithLabels(
                typeof(T).Namespace,
                typeof(T).Name,
                type).
                Inc(increment);
        }
        public void AddException(Exception e)
        {
            var s = new StackTrace(e, true);
            var frame = s.GetFrame(0);
            var file = frame.GetFileName()?.Split('\\')?.Last()??"n/a";

            ExceptionCounter.
                WithLabels(
                file,
                frame.GetFileLineNumber().ToString(),
                e.GetType().Namespace,
                e.GetType().Name).
                Inc();
        }
    }
}
