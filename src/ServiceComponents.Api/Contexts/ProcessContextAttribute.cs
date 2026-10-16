using System;

namespace ServiceComponents.Api.Contexts
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ProcessContextAttribute : Attribute
    {
        public string Title { get; }
        public string? ContextCode { get; }

        public ProcessContextAttribute(string title, string contextCode)
        {
            Title = title;
            ContextCode = contextCode;
        }

        public ProcessContextAttribute(string title)
        {
            Title = title;
        }
    }
}
