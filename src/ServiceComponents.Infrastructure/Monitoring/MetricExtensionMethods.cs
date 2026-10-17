using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ServiceComponents.Application.Monitoring;

namespace ServiceComponents.Infrastructure.Monitoring
{
    public static class MetricExtensionMethods
    {
        // GetProperties copies the property array on every call; the list (and its order) is fixed per type.
        // Types from collectible load contexts are not cached, so the cache never keeps them from unloading.
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PublicInstanceProperties = new();

        public static IEnumerable<string> MetricFields(this object obj)
        {
            return obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(info => info.GetCustomAttribute<MetricFieldAttribute>()?.Name ?? info.Name );
        }
        public static (string name, string title) MetricDescription(this object obj)
        {
            var type = obj.GetType();
            var attribute = type.GetCustomAttribute<MetricAttribute>();
            return (attribute == null ? type.Name : attribute.Name, attribute == null ? type.Namespace : attribute.Title);
        }

        public static IEnumerable<string> MetricValues(this object obj)
        {
            var type = obj.GetType();
            var properties = type.IsCollectible
                ? PublicInstancePropertiesOf(type)
                : PublicInstanceProperties.GetOrAdd(type, PublicInstancePropertiesOf);
            return properties.Select(info => info.GetValue(obj).ToString());
        }

        private static PropertyInfo[] PublicInstancePropertiesOf(Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        }

    }
}