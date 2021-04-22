﻿using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using ServiceComponents.AspNet.Http;

namespace ReferenceApplication2.AspNet.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GenericController : ControllerBase
    {
        private readonly ILogger _log;
        private readonly HttpRequestParser _httpRequestParser;
        private readonly IReceiveHttpCommand _commandReceiver;
        private readonly IReceiveHttpQuery _queryReceiver;
        private readonly IReceiveHttpEvent _eventReceiver;

        public GenericController(ILogger log, HttpRequestParser httpRequestParser, IReceiveHttpCommand commandReceiver, IReceiveHttpQuery queryReceiver, IReceiveHttpEvent eventReceiver)
        {
            _log = log;
            _httpRequestParser = httpRequestParser;
            _commandReceiver = commandReceiver;
            _queryReceiver = queryReceiver;
            _eventReceiver = eventReceiver;
            _httpRequestParser = httpRequestParser;
        }

        /// <summary>
        /// Generic method to post commands, queries and events
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Post(CancellationToken cancellationToken)
        {
            object result = null;

            await _httpRequestParser.Parse(HttpContext.Request,
                async (command, ct) => await _commandReceiver.ReceiveAsync(command, ct),
                async (query, ct) => result = await _queryReceiver.ReceiveAsync((dynamic)query, ct),
                async (@event, ct) => await _eventReceiver.ReceiveAsync(@event, ct),
                cancellationToken);

            return result == null ? (IActionResult)Ok() : Ok(result);
        }
    }
}
