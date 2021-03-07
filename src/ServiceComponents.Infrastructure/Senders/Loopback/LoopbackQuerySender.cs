using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using ServiceComponents.Api.Mediator;
using ServiceComponents.Application.Senders;
using ServiceComponents.Infrastructure.Receivers;

namespace ServiceComponents.Infrastructure.Senders
{
    public class LoopbackQuerySender : ISendLoopbackQuery
    {
        private readonly IReceiveLoopbackQuery _receiver;
        
        public LoopbackQuerySender(IReceiveLoopbackQuery receiver)
        {
            _receiver = receiver;
        }

        public async Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
        {
            return await _receiver.ReceiveAsync(query, cancellationToken);
        }
    }
}
