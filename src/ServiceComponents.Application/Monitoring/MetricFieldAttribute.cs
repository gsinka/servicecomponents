using System;

namespace ServiceComponents.Application.Monitoring
{
    [AttributeUsage(AttributeTargets.Property)]
    public class MetricFieldAttribute : Attribute
    {
        public string Name { get; }
        public string Title { get; }

        public MetricFieldAttribute(string name, string title = default)
        {
            Name = name;
            Title = title;
        }
    }
}