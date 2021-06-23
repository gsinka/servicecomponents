using System.ComponentModel.DataAnnotations;

namespace ServiceComponents.AspNetCore.Hosting.Options
{
    public class ApplicationOptions
    {
        [Required]
        public string Title { get; set; }
        public string Version { get; set; }
    }
}
