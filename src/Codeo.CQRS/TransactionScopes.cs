using System;
using System.Collections.Concurrent;
using System.Transactions;

namespace Codeo.CQRS
{
    public class TransactionScopes
    {
        /// <summary>
        /// Returns a transaction scope which supresses the ambient scope
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static TransactionScopeDecorator Suppress(
            int timeout = 30)
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
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static TransactionScopeDecorator ReadUncommitted(
            TransactionScopeOption option = TransactionScopeOption.Required,
            int timeout = 30)
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
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static TransactionScopeDecorator ReadCommitted(
            TransactionScopeOption option = TransactionScopeOption.Required,
            int timeout = 30)
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
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static TransactionScopeDecorator RepeatableRead(
            TransactionScopeOption option = TransactionScopeOption.Required,
            int timeout = 30)
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
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static TransactionScopeDecorator Serializable(
            TransactionScopeOption option = TransactionScopeOption.Required,
            int timeout = 30)
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
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static TransactionScopeDecorator Snapshot(
            TransactionScopeOption option = TransactionScopeOption.Required,
            int timeout = 30)
        {
            return Create(
                option,
                new TransactionOptions()
                {
                    IsolationLevel = IsolationLevel.Snapshot,
                    Timeout = TimeSpan.FromSeconds(timeout)
                });
            ;
        }

        /// <summary>
        /// Joins the ambient transaction (in the relevant isolation level), or creates a new transaction with the specified isolation level
        /// </summary>
        /// <param name="defaultLevelIfNoAmbient"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public static TransactionScopeDecorator JoinOrDefault(
            IsolationLevel defaultLevelIfNoAmbient,
            int timeout = 30)
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
        public static TransactionScopeDecorator Create(
            TransactionScopeOption scopeOption,
            TransactionOptions transactionOptions)
        {
            return new TransactionScope(
                scopeOption,
                transactionOptions
            ).ProtectAgainstDisposalExceptionsWith(ScopeDisposalExceptionHandlers);
        }

        private static ConcurrentDictionary<Guid, Func<Exception, DisposalExceptionHandlerResults>>
            ScopeDisposalExceptionHandlers =
                new ConcurrentDictionary<Guid, Func<Exception, DisposalExceptionHandlerResults>>();

        public static Guid InstallDisposalExceptionHandler(
            Func<Exception, DisposalExceptionHandlerResults> handler)
        {
            var id = Guid.NewGuid();
            ScopeDisposalExceptionHandlers.TryAdd(id, handler);
            return id;
        }

        public static void UninstallDisposalExceptionHandler(
            Guid id)
        {
            ScopeDisposalExceptionHandlers.TryRemove(id, out var _);
        }

    }
}