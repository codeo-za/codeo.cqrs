using System;

namespace Codeo.CQRS.Exceptions
{
    /// <summary>
    /// This exception should be thrown when a UNIQUE database constraint is violated
    /// </summary>
    public class UniqueConstraintViolationException : DatabaseException
    {
        /// <inheritdoc />
        public UniqueConstraintViolationException(
            Operation operation,
            string queryDescriptor,
            object predicate,
            Exception ex
        ) : base(operation, queryDescriptor, predicate, ex)
        {
        }
    }
}