using System;
using System.Runtime.Serialization;

namespace Codeo.CQRS.Exceptions
{
    /// <summary>
    /// This exception should be thrown when a UNIQUE database constraint is violated
    /// </summary>
    [Serializable]
    public class UniqueConstraintViolationException : DatabaseException
    {
        protected UniqueConstraintViolationException(SerializationInfo info, StreamingContext streamingContext) : base(info,
            streamingContext)
        {
            // for serializable logging support.  
        }
        
        public UniqueConstraintViolationException(Operation operation, string queryDescriptor, object predicate, Exception ex)
            : base(operation, queryDescriptor, predicate, ex)
        {
        }
    }
}