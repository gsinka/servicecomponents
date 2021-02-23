using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using ReferenceApplication.Api;
using Serilog.Data;
using ServiceComponents.Application.Senders;
using ServiceComponents.Infrastructure.CorrelationContext;
using ServiceComponents.Infrastructure.Http;
using ILogger = Serilog.ILogger;

namespace ReferenceApplication.AspNet.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;
        private readonly ISendCommand _commandSender;
        private readonly Correlation _correlation;


        public WeatherForecastController(ILogger<WeatherForecastController> logger, ISendCommand commandSender, Correlation correlation)
        {
            _logger = logger;
            _commandSender = commandSender;
            _correlation = correlation;
        }

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            var command = new TestCommand("Data");

            _correlation.CorrelationId = Guid.NewGuid().ToString();
            _correlation.CausationId = Guid.NewGuid().ToString();
            _correlation.CurrentId = command.CommandId;

            _logger.LogInformation("correlation: {@correlation}", _correlation);
            
            _commandSender.SendAsync(command);
            
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = Summaries[rng.Next(Summaries.Length)]
            })
            .ToArray();
        }
    }
}
