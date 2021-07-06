using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace ServiceComponents.Infrastructure.Options
{
    public class ApplicationOptions
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string ShortName { get; set; }
        public string Description { get; set; }

        public static string InformationalVersion => Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        public static string FullSemVer => Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion.Split('+', 2)[0];
        public static string Version => Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
        public static string ApiVersion => Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>().Version.Split('.')[0];
    }
}
