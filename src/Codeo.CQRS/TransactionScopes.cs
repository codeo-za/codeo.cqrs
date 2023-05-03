using System;
using System.Collections.Concurrent;
using System.Transactions;

namespace Codeo.CQRS
{
    /// <summary>
    /// 
    /// </summary>
    public class TransactionScopes
    {
        /// <summary>
        /// The default timeout, in seconds, for
        /// transactions
        /// </summary>
        public const int DEFAULT_TIMEOUT_SECONDS = 30;

        /// <summary>
        /// Returns a transaction scope which suppresses the ambient scope
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static ITransactionScope Suppress(
            int timeout = DEFAULT_TIMEOUT_SECONDS
        )
        {
            return Create(
                TransactionScopeOption.Suppress,
                new TransactionOptions()
                {
                    Timeout = TimeSpan.FromSeconds(timeout)
                });
        }

        /// <summary>
        /// Returns a transaction scope with the relevant timeout in seconds
        /// </summary>
        /// <param name="option"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static ITransactionScope ReadUncommitted(
            TransactionScopeOption option = TransactionScopeOption.Required,
            int timeout = DEFAULT_TIMEOUT_SECONDS)
        {
            return Create(
                option,
                new TransactionOptions()
                {
                    IsolationLevel = IsolationLevel.ReadUncommitted,
                    Timeout = TimeSpan.FromSeconds(timeout)
                });
        }

        /// <summary>
        /// Returns a transaction scope with the relevant timeout in seconds
        /// </summary>
        /// <param name="option"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static ITransactionScope ReadCommitted(
            TransactionScopeOption option = TransactionScopeOption.Required,
            int timeout = DEFAULT_TIMEOUT_SECONDS)
        {
            return Create(
                option,
                new TransactionOptions()
                {
                    IsolationLevel = IsolationLevel.ReadCommitted,
                    Timeout = TimeSpan.FromSeconds(timeout)
                });
        }

        /// <summary>
        /// Returns a transaction scope with the relevant timeout in seconds
        /// </summary>
        /// <param name="option"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static ITransactionScope RepeatableRead(
            TransactionScopeOption option = TransactionScopeOption.Required,
            int timeout = DEFAULT_TIMEOUT_SECONDS)
        {
            return Create(
                option,
                new TransactionOptions()
                {
                    IsolationLevel = IsolationLevel.RepeatableRead,
                    Timeout = TimeSpan.FromSeconds(timeout)
                });
        }

        /// <summary>
        /// Returns a transaction scope with the relevant timeout in seconds
        /// </summary>
        /// <param name="option"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static ITransactionScope Serializable(
            TransactionScopeOption option = TransactionScopeOption.Required,
            int timeout = DEFAULT_TIMEOUT_SECONDS)
        {
            return Create(
                option,
                new TransactionOptions()
                {
                    IsolationLevel = IsolationLevel.Serializable,
                    Timeout = TimeSpan.FromSeconds(timeout)
                });
        }

        /// <summary>
        /// Returns a transaction scope with the relevant timeout in seconds
        /// </summary>
        /// <param name="option"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static ITransactionScope Snapshot(
            TransactionScopeOption option = TransactionScopeOption.Required,
            int timeout = DEFAULT_TIMEOUT_SECONDS)
        {
            return Create(
                option,
                new TransactionOptions()
                {
                    IsolationLevel = IsolationLevel.Snapshot,
                    Timeout = TimeSpan.FromSeconds(timeout)
                });
        }

        /// <summary>
        /// Joins the ambient transaction (in the relevant isolation level), or creates a new transaction with the specified isolation level
        /// </summary>
        /// <param name="defaultLevelIfNoAmbient"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static ITransactionScope JoinOrDefault(
            IsolationLevel defaultLevelIfNoAmbient,
            int timeout = DEFAULT_TIMEOUT_SECONDS)
        {
            return Create(TransactionScopeOption.Required,
                new TransactionOptions()
                {
                    IsolationLevel = Transaction.Current == null
                        ? defaultLevelIfNoAmbient
                        : Transaction.Current.IsolationLevel,
                    Timeout = TimeSpan.FromSeconds(timeout)
                });
        }

        /// <summary>
        /// Create a transaction with the specified scope and transaction options
        /// </summary>
        /// <param name="scopeOption"></param>
        /// <param name="transactionOptions"></param>
        /// <returns></returns>
        public static ITransactionScope Create(
            TransactionScopeOption scopeOption,
            TransactionOptions transactionOptions)
        {
            return new TransactionScope(
                scopeOption,
                transactionOptions
            ).ProtectAgainstDisposalExceptionsWith(ScopeDisposalExceptionHandlers);
        }

        private static readonly ConcurrentDictionary<Guid, Func<Exception, DisposalExceptionHandlerResults>>
            ScopeDisposalExceptionHandlers =
                new ConcurrentDictionary<Guid, Func<Exception, DisposalExceptionHandlerResults>>();

        /// <summary>
        /// Install an exception handler for disposal-time exceptions
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public static Guid InstallDisposalExceptionHandler(
            Func<Exception, DisposalExceptionHandlerResults> handler)
        {
            var id = Guid.NewGuid();
            ScopeDisposalExceptionHandlers.TryAdd(id, handler);
            return id;
        }

        /// <summary>
        /// Remove the provided disposal-time exception handler, if it is found
        /// </summary>
        /// <param name="id"></param>
        public static void UninstallDisposalExceptionHandler(
            Guid id)
        {
            ScopeDisposalExceptionHandlers.TryRemove(id, out var _);
        }
    }
}