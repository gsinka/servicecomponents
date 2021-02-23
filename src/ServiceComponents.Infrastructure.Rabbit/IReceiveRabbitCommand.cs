﻿using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client.Events;
using ServiceComponents.Api.Mediator;

namespace ServiceComponents.Infrastructure.Rabbit
{
    public interface IReceiveRabbitCommand
    {
        Task ReceiveAsync<T>(T command, BasicDeliverEventArgs args, CancellationToken cancellationToken) where T : ICommand;
    }
}