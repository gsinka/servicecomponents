using System.Diagnostics;
using System.Reflection;

namespace ServiceComponents.Core.Helpers
{
    public static class VersionHelpers
    {
        public static string AssemblyVersion(this Assembly assembly)
        {
            return assembly.GetName().Version.ToString();
        }

        public static string FileVersion(this Assembly assembly)
        {
            return FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion;
        }
        public static string ProductVersion(this Assembly assembly)
        {
            return FileVersionInfo.GetVersionInfo(assembly.Location).ProductVersion;
        }
    }
}
