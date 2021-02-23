using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReferenceApplication.Api;
using ServiceComponents.Infrastructure.Http;

namespace ReferenceApplication.AspNet.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProtectedController : ControllerBase
    {
        private readonly IReceiveHttpQuery _queryReceiver;

        public ProtectedController(IReceiveHttpQuery queryReceiver)
        {
            _queryReceiver = queryReceiver;
        }

        public IActionResult Get()
        {
            return Ok("Done");
        }

        [HttpPost]
        public async Task<IActionResult> Query([FromBody]TestQuery query, CancellationToken cancellationToken)
        {
            return Ok(await _queryReceiver.ReceiveAsync(query, cancellationToken));
        }
    }
}
