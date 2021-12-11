using System;
using System.Runtime.Serialization;

namespace ServiceComponents.Core.Exceptions
{
    public class NotFoundException : BusinessException
    {
        public NotFoundException()
        { }

        protected NotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        { }

        public NotFoundException(string message) : base(404, message)
        { }

        public NotFoundException(int errorCode, string message) : base(errorCode, message)
        { }

        public NotFoundException(string message, Exception innerException) : base(message, innerException)
        { }
    }
}