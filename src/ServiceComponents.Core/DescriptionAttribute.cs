using System;

namespace ServiceComponents.Core
{
    [AttributeUsage(AttributeTargets.All)]
    public class DescriptionAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }

        public DescriptionAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}
