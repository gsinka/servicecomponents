using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using ServiceComponents.Api.Mediator;
using StackExchange.Redis;

namespace ServiceComponents.Infrastructure.Redis.Scheduler
{
    public class SchedulerService : BackgroundService
    {
        private readonly IConnectionMultiplexer _connection;
        private readonly int _database;

        public SchedulerService(IConnectionMultiplexer connection, int database)
        {
            _connection = connection;
            _database = database;
        }

        override protected async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested) {

                var db = _connection.GetDatabase(_database);
            }
        }

        public void ScheduleCommand(ICommand command, DateTime schedule)
        {

        }
    }

    public class ScheduledJob
    {
        public DateTime Schedule { get; }
        
        public ScheduledJob(DateTime schedule)
        {
            Schedule = schedule;
        }
    }
}
