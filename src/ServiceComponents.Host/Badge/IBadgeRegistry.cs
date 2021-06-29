using System;
using System.Threading.Tasks;

namespace ServiceComponents.Host.Badge
{
    public interface IBadgeRegistry
    {
        void RegistrateVersionBadge(string version = default);
        void RegistrateReadinessBadge();
        void RegistrateLivenessBadge();
        void RegistrateBadge(string key, string label, Func<Task<string>> status, string color);
        void RegistrateBadge(string key, string label, Func<Task<string>> status, Func<string, string> color);
    }
}