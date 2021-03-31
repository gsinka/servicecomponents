using System;
using System.Runtime.Serialization;
using ServiceComponents.Core.Exceptions;

namespace ServiceComponents.Infrastructure.Behaviors.CommandConstraints
{
    public class CommandConstraintException : BusinessException
    {
        public CommandConstraintException()
        {
        }

        protected CommandConstraintException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public CommandConstraintException(string message) : base(message)
        {
        }

        public CommandConstraintException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
