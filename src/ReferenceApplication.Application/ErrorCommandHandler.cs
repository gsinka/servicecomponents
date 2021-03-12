using System;
using System.Threading;
using System.Threading.Tasks;
using ReferenceApplication.Api;
using ServiceComponents.Application.Mediator;
using ServiceComponents.Core.Exceptions;

namespace ReferenceApplication.Application
{
    public class ErrorCommandHandler : IHandleCommand<ErrorCommand>
    {
        public Task HandleAsync(ErrorCommand command, CancellationToken cancellationToken = default)
        {
            throw command.Code switch {
                400 => (Exception)new BusinessException(command.Message),
                404 => new NotFoundException(command.Message),
                _ => new InvalidOperationException("Unknown error code")
            };
        }
    }
}