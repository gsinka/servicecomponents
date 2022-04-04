using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ReferenceApplication.Api;
using Serilog;
using ServiceComponents.Application;
using ServiceComponents.Application.Senders;
using ServiceComponents.AspNet.Http;
using ServiceComponents.AspNet.Services;
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
        private readonly IEventRecorder _eventRecorder;
        private readonly IBackgroundTaskQueue _taskQueue;

        public TestController(IReceiveHttpCommand httpCommandReceiver, ISendCommand commandSender, Correlation correlation, IEventRecorder eventRecorder, IBackgroundTaskQueue taskQueue)
        {
            _httpCommandReceiver = httpCommandReceiver;
            _commandSender = commandSender;
            _correlation = correlation;
            _eventRecorder = eventRecorder;
            _taskQueue = taskQueue;
        }

        [HttpGet("get")]
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

        [HttpGet("testcor")]
        public async Task<IActionResult> TestCor(CancellationToken cancellationToken)
        {
            await _commandSender.SendAsync(new TestCommand("testcor"), cancellationToken);
            return Ok();
        }

        [HttpPost("queueTask")]
        public async Task<IActionResult> AddBackgroundTask(CancellationToken cancellationToken)
        {
            await _taskQueue.QueueBackgroundWorkItemAsync(async token => {

                var taskId = Guid.NewGuid().ToString();
                Log.Logger.Information($"Task {taskId} started");
                await Task.Delay(5000, cancellationToken);
                Log.Logger.Information($"Task {taskId} finished");
            });

            return Accepted();
        }

    }
}
