using System;

namespace ServiceComponents.Api.Mediator
{
    public abstract class Command : ICommand
    {
        public string CommandId { get; }

        protected Command(string commandId = null)
        {
            CommandId = commandId ?? Guid.NewGuid().ToString();
        }
        
    }
}