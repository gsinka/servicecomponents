using System;
using System.Threading.Tasks;

namespace ServiceComponents.Host.Badge
{
    internal class BadgeDefinition
    {
        public string Label { get; }
        public Func<Task<string>> Status { get; }
        public Func<string, string> Color { get; }

        public BadgeDefinition(string label, Func<Task<string>> status, Func<string, string> color)
        {
            Label = label;
            Status = status;
            Color = color;
        }        
    }
}