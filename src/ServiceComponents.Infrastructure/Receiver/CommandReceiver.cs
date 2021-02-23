using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Infrastructure.HandlerContext;
using ServiceComponents.Infrastructure.Mediator;
using ServiceComponents.Infrastructure.Mediator.Dispatchers;

namespace ServiceComponents.Infrastructure.Receiver
{
    public class CommandReceiver : IReceiveCommand
    {
        public Task ReceiveAsync(ICommand command, string correlationId = null, string causationId = null, string userId = null,
            string userName = null, CancellationToken cancellationToken = default)
        {
            throw new System.NotImplementedException();
        }
    }

    public class QueryReceiver : IReceiveQuery
    //public class CommandReceiver : IReceiveCommand
    //{
    //    private readonly IDispatchCommand _commandDispatcher;
    //    private readonly CommandContext _context;

    //    public CommandReceiver(IDispatchCommand commandDispatcher, CommandContext context)
    //    {
    //        _commandDispatcher = commandDispatcher;
    //        _context = context;
    //    }

    //    public async Task ReceiveAsync(ICommand command, string correlationId = null, string causationId = null, string userId = null, string userName = null, CancellationToken cancellationToken = default)
    //    {
    //        if (command != null) _context.Command = command;
    //        if (correlationId != null) _context.CorrelationId = correlationId;
    //        if (causationId != null) _context.CausationId = causationId;
    //        if (userId != null) _context.UserId = userId;
    //        if (userName != null) _context.UserName = userName;

    //        await _commandDispatcher.DispatchAsync(command, cancellationToken);
    //    }
    //}
}