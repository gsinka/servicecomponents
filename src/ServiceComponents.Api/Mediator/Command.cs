using System;

namespace ServiceComponents.Api.Mediator
{
    public abstract class Command : ICommand
    {
        public string CommandId { get; }

        protected Command() : this(default) { }

        protected Command(string commandId)
        {
            CommandId = commandId ?? Guid.NewGuid().ToString();
        }
        
    }
}