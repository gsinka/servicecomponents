using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ReferenceApplication.Api;
using ServiceComponents.Application;
using ServiceComponents.Application.Senders;
using ServiceComponents.AspNet;
using ServiceComponents.AspNet.EventRecorder;
using ServiceComponents.AspNet.Http;
using ServiceComponents.Infrastructure.CorrelationContext;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly IReceiveHttpCommand _httpCommandReceiver;
        private readonly ISendCommand _commandSender;
        private readonly Correlation _correlation;
        private readonly EventRecorderService _eventRecorder;

        public TestController(IReceiveHttpCommand httpCommandReceiver, ISendCommand commandSender, Correlation correlation, EventRecorderService eventRecorder)
        {
            _httpCommandReceiver = httpCommandReceiver;
            _commandSender = commandSender;
            _correlation = correlation;
            _eventRecorder = eventRecorder;
        }

        [HttpGet]
        public async Task<IActionResult> Get(CancellationToken cancellationToken)
        {
            _correlation.CorrelationId = "awaiter";

            var awaiter = _eventRecorder.WaitFor<TestEvent>(
                (evnt, correlation) => evnt is TestEvent && correlation.CorrelationId == "awaiter", 
                TimeSpan.FromSeconds(5));

            var testCommand = new TestCommand("awaiterTest");
            await _commandSender.SendAsync(testCommand, cancellationToken);

            await awaiter;

            return Ok();
            return awaiter.IsCompletedSuccessfully ? Ok(awaiter.Result.Data) : BadRequest("Event never arrived");
        }

        public async Task<IActionResult> Post(TestCommand testCommand, CancellationToken cancellationToken)
        {
            var awaiter = _eventRecorder.WaitFor<TestEvent>(
                (evnt, correlation) => evnt is TestEvent && correlation.CorrelationId == HttpContext.Request.Headers["correlation-id"],
                TimeSpan.FromSeconds(5));

            await _httpCommandReceiver.ReceiveAsync(testCommand, cancellationToken);

            await awaiter;

            return awaiter.Result != null ? Ok() : BadRequest();
        }

    }
}
