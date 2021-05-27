using System;
using System.Runtime.Serialization;

namespace ServiceComponents.Core.Exceptions
{
    public class BusinessException : Exception
    {
        public int ErrorCode { get; }

        public BusinessException()
        { }

        protected BusinessException(SerializationInfo info, StreamingContext context) : base(info, context)
        { }

        public BusinessException(string message) : base(message)
        { }

        public BusinessException(string message, Exception innerException) : base(message, innerException)
        { }

        public BusinessException(int errorCode, string message) : base(message)
        {
            ErrorCode = errorCode;
        }

        public BusinessException(int errorCode, string message, Exception innerException) : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }
}
