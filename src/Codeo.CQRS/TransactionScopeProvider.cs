using System.Transactions;
using static Codeo.CQRS.TransactionScopes;

namespace Codeo.CQRS
{
    /// <summary>
    /// Injectable service to provide transaction scopes, allowing
    /// test verification that code is run in a transaction and that
    /// transactions are completed
    /// </summary>
    public interface ITransactionScopeProvider
    {
        /// <summary>
        /// Suppress the transaction for the current scope
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        ITransactionScope Suppress(int timeout = DEFAULT_TIMEOUT_SECONDS);

        /// <summary>
        /// Create a transaction scope with Repeatable Read isolation
        /// </summary>
        /// <param name="option"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        ITransactionScope ReadUncommitted(
            TransactionScopeOption option = TransactionScopeOption.Required,
            int timeout = DEFAULT_TIMEOUT_SECONDS
        );

        /// <summary>
        /// Create a transaction scope with Read Committed isolation
        /// </summary>
        /// <param name="option"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        ITransactionScope ReadCommitted(
            TransactionScopeOption option = TransactionScopeOption.Required,
            int timeout = DEFAULT_TIMEOUT_SECONDS
        );

        /// <summary>
        /// Create a transaction scope with Repeatable Read isolation
        /// </summary>
        /// <param name="option"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        ITransactionScope RepeatableRead(
            TransactionScopeOption option = TransactionScopeOption.Required,
            int timeout = DEFAULT_TIMEOUT_SECONDS
        );

        /// <summary>
        /// Create a transaction scope with Serializable isolation
        /// </summary>
        /// <param name="option"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        ITransactionScope Serializable(
            TransactionScopeOption option = TransactionScopeOption.Required,
            int timeout = DEFAULT_TIMEOUT_SECONDS
        );

        /// <summary>
        /// Create a transaction scope with Snapshot isolation
        /// </summary>
        /// <param name="option"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        ITransactionScope Snapshot(
            TransactionScopeOption option = TransactionScopeOption.Required,
            int timeout = 30
        );

        /// <summary>
        /// Joins any existing transaction scope with the provided
        /// isolation level or higher. Will start a new transaction
        /// if none is currently in-flight.
        /// </summary>
        /// <param name="isolationLevel"></param>
        /// <returns></returns>
        ITransactionScope JoinOrDefault(IsolationLevel isolationLevel);
    }

    /// <summary>
    /// Injectable service to provide transaction scopes
    /// </summary>
    public class TransactionScopeProvider : ITransactionScopeProvider
    {
        /// <inheritdoc />
        public ITransactionScope Suppress(
            int timeout = DEFAULT_TIMEOUT_SECONDS
        )
        {
            return TransactionScopes.Suppress(timeout);
        }

        /// <inheritdoc />
        public ITransactionScope ReadUncommitted(
            TransactionScopeOption option = TransactionScopeOption.Required,
            int timeout = DEFAULT_TIMEOUT_SECONDS
        )
        {
            return TransactionScopes.ReadUncommitted(
                option, timeout
            );
        }

        /// <inheritdoc />
        public ITransactionScope ReadCommitted(
            TransactionScopeOption option = TransactionScopeOption.Required,
            int timeout = DEFAULT_TIMEOUT_SECONDS
        )
        {
            return TransactionScopes.ReadCommitted(
                option, timeout
            );
        }

        /// <inheritdoc />
        public ITransactionScope RepeatableRead(
            TransactionScopeOption option = TransactionScopeOption.Required,
            int timeout = DEFAULT_TIMEOUT_SECONDS
        )
        {
            return TransactionScopes.RepeatableRead(
                option, timeout
            );
        }

        /// <inheritdoc />
        public ITransactionScope Serializable(
            TransactionScopeOption option = TransactionScopeOption.Required,
            int timeout = DEFAULT_TIMEOUT_SECONDS
        )
        {
            return TransactionScopes.Serializable(
                option, timeout
            );
        }

        /// <inheritdoc />
        public ITransactionScope Snapshot(
            TransactionScopeOption option = TransactionScopeOption.Required,
            int timeout = 30
        )
        {
            return TransactionScopes.Snapshot(
                option, timeout
            );
        }

        /// <inheritdoc />
        public ITransactionScope JoinOrDefault(
            IsolationLevel isolationLevel
        )
        {
            return TransactionScopes.JoinOrDefault(isolationLevel);
        }
    }
}