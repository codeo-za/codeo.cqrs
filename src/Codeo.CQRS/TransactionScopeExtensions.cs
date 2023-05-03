using System;
using System.Collections.Concurrent;
using System.Transactions;

namespace Codeo.CQRS
{
    /// <summary>
    /// Provides extension methods for TransactionScope objects
    /// </summary>
    public static class TransactionScopeExtensions
    {
        /// <summary>
        /// Adds an exception handler for disposal-time exceptions
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="disposalExceptionHandlers"></param>
        /// <returns></returns>
        public static TransactionScopeDecorator ProtectAgainstDisposalExceptionsWith(
            this TransactionScope scope,
            ConcurrentDictionary<Guid, Func<Exception, DisposalExceptionHandlerResults>> disposalExceptionHandlers)
        {
            return new TransactionScopeDecorator(scope, disposalExceptionHandlers);
        }
    }
}