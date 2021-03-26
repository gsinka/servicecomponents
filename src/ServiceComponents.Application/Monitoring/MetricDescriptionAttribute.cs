using System;

namespace ServiceComponents.Application.Monitoring
{
    [AttributeUsage(AttributeTargets.Class)]
    public abstract class MetricAttribute : Attribute
    {
        public string Name { get; }
        public string Title { get; }

        protected MetricAttribute(string name, string title)
        {
            Name = name;
            Title = title;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class CounterMetric : MetricAttribute
    {
        public CounterMetric(string name, string title) : base(name, title)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class SummaryMetric : MetricAttribute
    {
        public SummaryMetric(string name, string title) : base(name, title)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public abstract class HistogramMetric : MetricAttribute
    {
        public double Start { get; }
        public int Count { get; }

        protected HistogramMetric(string name, string title, double start, int count) : base(name, title)
        {
            Start = start;
            Count = count;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class LinearHistogramMetric : HistogramMetric
    {
        public double Width { get; }

        public LinearHistogramMetric(string name, string title, double start, double width, int count) : base(name, title, start, count)
        {
            Width = width;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ExponentialHistogramMetric : HistogramMetric
    {
        public double Factor { get; }
        
        public ExponentialHistogramMetric(string name, string title, double start, double factor, int count) : base(name, title, start, count)
        {
            Factor = factor;
        }
    }
}