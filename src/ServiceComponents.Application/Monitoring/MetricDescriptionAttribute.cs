using System;

namespace ServiceComponents.Application.Monitoring
{
    [AttributeUsage(AttributeTargets.Class)]
    public class MetricDescriptionAttribute : Attribute
    {
        public string Name { get; }
        public string Title { get; }

        public MetricDescriptionAttribute(string name, string title)
        {
            Name = name;
            Title = title;
        }
    }
}