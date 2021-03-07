﻿using System.Threading;
using System.Threading.Tasks;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Infrastructure.Receivers
{
    public interface IReceiveLoopbackQuery
    {
        Task<TResult> ReceiveAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default);
    }
}