using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace ServiceComponents.Host.Badge
{
    public class BadgeController : Controller
    {
        private readonly BadgeService _badgeService;

        public BadgeController(BadgeService badgeService)
        {
            _badgeService = badgeService;
        }
        
        public async Task<IActionResult> Get([FromRoute(Name = "name")]string name)
        {
            return string.IsNullOrWhiteSpace(name) ? await _badgeService.GetAllBadgeAsync() : await _badgeService.GetBadgeAsync(name);
        }
    }
}
