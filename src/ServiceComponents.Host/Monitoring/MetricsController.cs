using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace ServiceComponents.Host.Monitoring
{
    [Route("metrics")]
    [ApiController]
    public class MetricsController : ControllerBase
    {
        /// <summary>
        /// Provides metrics
        /// </summary>
        /// <returns></returns>
        // GET /metrics
        [HttpGet()]
        public async Task<ActionResult<string>> GetMetrics()
        {
            var stream = new MemoryStream();
            await Prometheus.Metrics.DefaultRegistry.CollectAndExportAsTextAsync(stream);
            stream.Position = 0;
            
            var reader = new StreamReader(stream);
            var result = await reader.ReadToEndAsync();
            return result;
        }
    }

}
