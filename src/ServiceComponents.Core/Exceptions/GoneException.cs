using System;
using System.Runtime.Serialization;

namespace ServiceComponents.Core.Exceptions
{
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