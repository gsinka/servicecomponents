using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ReferenceApplication.Api;
using ServiceComponents.AspNet.Http;
using ServiceComponents.Core.Services;

namespace ReferenceApplication2.AspNet.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly IClock _clock;
        private readonly IReceiveHttpCommand _receiver;

        public TestController(IClock clock, IReceiveHttpCommand receiver)
        {
            _clock = clock;
            _receiver = receiver;
        }

        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            await _receiver.ReceiveAsync(new TestCommand("data"), cancellationToken);
            return Ok($"OK : {_clock.UtcNow.ToString(CultureInfo.InvariantCulture)}");
        }
    }
}
