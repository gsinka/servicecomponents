using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ServiceComponents.Application.Monitoring;

namespace ServiceComponents.Infrastructure.Monitoring
{
    public static class MetricExtensionMethods
    {
        public static IEnumerable<string> MetricFields(this object obj)
        {
            return obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(info => info.GetCustomAttribute<MetricFieldAttribute>()?.Name ?? info.Name );
        }
        public static (string name, string title) MetricDescription(this object obj)
        {
            var type = obj.GetType();
            var attribute = type.GetCustomAttribute<MetricDescriptionAttribute>();
            return (attribute == null ? type.Name : attribute.Name, attribute == null ? type.Namespace : attribute.Title);
        }

        public static IEnumerable<string> MetricValues(this object obj)
        {
            var type = obj.GetType();
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(info => info.GetValue(obj).ToString());
        }

    }
}