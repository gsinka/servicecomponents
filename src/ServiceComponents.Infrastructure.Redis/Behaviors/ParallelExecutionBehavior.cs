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
        private readonly IDatabaseAsync _redisDatabase;
        private readonly Func<IList<ICommand>, bool> _enablerFunc;
        private readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.All };


        public ParallelExecutionBehavior(IDatabaseAsync redisDatabase, Func<IList<ICommand>, bool> enablerFunc)
        {
            _redisDatabase = redisDatabase;
            _enablerFunc = enablerFunc;
        }
        
        public async Task PreHandleAsync(ICommand command, CancellationToken cancellationToken = default)
        {
            var executingCommands = 
                (await _redisDatabase.HashGetAllAsync(new RedisKey("commands")))
                .Select(hash => JsonConvert.DeserializeObject(hash.Value, _jsonSerializerSettings) as ICommand)
                .Where(x => x != null)
                .ToList();

            if (!_enablerFunc(executingCommands)) {
                throw new InvalidOperationException("Cannot execute command because of parallel constraints");
            }

            await _redisDatabase.HashSetAsync(new RedisKey("commands"), new[] { new HashEntry(new RedisValue(command.CommandId), new RedisValue(JsonConvert.SerializeObject(command, _jsonSerializerSettings)))});
        }

        public async Task PostHandleAsync(ICommand command, CancellationToken cancellationToken = default)
        {
            await _redisDatabase.HashDeleteAsync(new RedisKey("commands"), new RedisValue(command.CommandId));
        }

        public async Task HandleFailureAsync(ICommand command, Exception exception, CancellationToken cancellationToken = default)
        {
            await _redisDatabase.HashDeleteAsync(new RedisKey(command.CommandId), new RedisValue(command.CommandId));
        }
    }
}