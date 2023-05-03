using System;
using System.Collections.Concurrent;
using System.Transactions;

namespace Codeo.CQRS
{
    /// <summary>
    /// Contract for a transaction scope, allowing completion
    /// and providing for disposal
    /// </summary>
    public interface ITransactionScope : IDisposable
    {
        /// <summary>
        /// Commit the transaction
        /// </summary>
        void Complete();
    }

    /// <summary>
    /// Possible return values from exception handlers
    /// registered for exceptions thrown during transaction
    /// disposal
    /// </summary>
    public enum DisposalExceptionHandlerResults
    {
        /// <summary>
        /// The error has been handled - do not rethrow
        /// </summary>
        Handled,
        /// <summary>
        /// The error has not been handled - if no other
        /// handler has handled it, rethrow
        /// </summary>
        Unhandled
    }

    /// <summary>
    /// Decorates a transaction scope, allowing the
    /// insertion of default exception handlers (eg
    /// the famous NullReferenceException from
    /// a timed-out MySql transaction)
    /// </summary>
    public class TransactionScopeDecorator : ITransactionScope
    {
        private readonly TransactionScope _scope;

        private readonly ConcurrentDictionary<Guid, Func<Exception, DisposalExceptionHandlerResults>>
            _disposalExceptionHandlers;

        internal TransactionScopeDecorator(
            TransactionScope scope,
            ConcurrentDictionary<Guid, Func<Exception, DisposalExceptionHandlerResults>> disposalExceptionHandlers)
        {
            _scope = scope;
            _disposalExceptionHandlers = disposalExceptionHandlers;
        }

        /// <summary>
        /// Completes the underlying transaction
        /// </summary>
        public void Complete()
        {
            _scope.Complete();
        }

        /// <summary>
        /// Disposes the underlying transaction, applying
        /// registered exception handlers as necessary
        /// </summary>
        public void Dispose()
        {
            try
            {
                _scope.Dispose();
            }
            catch (Exception ex)
            {
                foreach (var handler in _disposalExceptionHandlers)
                {
                    if (handler.Value(ex) == DisposalExceptionHandlerResults.Handled)
                    {
                        return;
                    }
                }

                throw;
            }
        }
    }

}