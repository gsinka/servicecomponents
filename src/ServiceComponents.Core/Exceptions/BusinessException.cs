using System;
using System.Runtime.Serialization;

namespace ServiceComponents.Core.Exceptions
{
    public class BusinessException : Exception
    {
        public BusinessException()
        { }

        protected BusinessException(SerializationInfo info, StreamingContext context) : base(info, context)
        { }

        public BusinessException(string message) : base(message)
        { }

        public BusinessException(string message, Exception innerException) : base(message, innerException)
        { }
    }

    public class NotFoundException : BusinessException
    {
        public NotFoundException()
        { }

        protected NotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        { }

        public NotFoundException(string message) : base(message)
        { }

        public NotFoundException(string message, Exception innerException) : base(message, innerException)
        { }
    }

    public class GoneException : BusinessException
    {
        public GoneException()
        { }

        protected GoneException(SerializationInfo info, StreamingContext context) : base(info, context)
        { }

        public GoneException(string message) : base(message)
        { }

        public GoneException(string message, Exception innerException) : base(message, innerException)
        { }
    }


}
