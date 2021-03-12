using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Mediator;
using StackExchange.Redis;

namespace ServiceComponents.Infrastructure.Redis.Behaviors
{
    public class ParallelExecutionBehavior : IPreHandleCommand, IPostHandleCommand, IHandleCommandFailure
    {
        private readonly IDatabase _database;
        private readonly Func<ICommand, IList<ICommand>, bool> _enablerFunc;
        private readonly Func<ICommand, TimeSpan?> _expiryFunc;
        private readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All };


        public ParallelExecutionBehavior(IDatabase database, Func<ICommand, IList<ICommand>, bool> enablerFunc, Func<ICommand, TimeSpan?> expiryFunc)
        {
            _enablerFunc = enablerFunc;
            _expiryFunc = expiryFunc;
            _database = database;
        }
        
        public async Task PreHandleAsync(ICommand command, CancellationToken cancellationToken = default)
        {
            var connection = _database.Multiplexer;
            var runningKeys = connection.GetServer(connection.GetEndPoints().First()).Keys(database: _database.Database, pattern: "command:*");
            var executingCommands = runningKeys.Select(key => JsonConvert.DeserializeObject(_database.StringGet(key), _jsonSerializerSettings) as ICommand).ToList();

            if (!_enablerFunc(command, executingCommands)) {
                throw new InvalidOperationException("Cannot execute command because of parallel constraints");
            }

            await _database.StringSetAsync($"command:{command.CommandId}", new RedisValue(JsonConvert.SerializeObject(command, _jsonSerializerSettings)), _expiryFunc(command));
        }

        public async Task PostHandleAsync(ICommand command, CancellationToken cancellationToken = default)
        {
            await _database.KeyDeleteAsync(new RedisKey($"command:{command.CommandId}"));
        }

        public async Task HandleFailureAsync(ICommand command, Exception exception, CancellationToken cancellationToken = default)
        {
            await _database.KeyDeleteAsync(new RedisKey($"command:{command.CommandId}"));
        }
    }
}