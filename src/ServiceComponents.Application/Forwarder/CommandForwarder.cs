using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Mediator;
using ServiceComponents.Application.Senders;

namespace ServiceComponents.Application.Forwarder
{
    public abstract class CommandForwarder<TIn, TOut> : IHandleCommand<TIn> where TIn : ICommand where TOut : ICommand
    {
        protected readonly ISendCommand CommandSender;

        protected CommandForwarder(ISendCommand commandSender)
        {
            CommandSender = commandSender;
        }

        public async Task HandleAsync(TIn command, CancellationToken cancellationToken = default)
        {
            var outCommand = await Transform(command, cancellationToken);
            await CommandSender.SendAsync(outCommand, cancellationToken);
        }

        public abstract Task<TOut> Transform(TIn input, CancellationToken cancellationToken);
    }
}
