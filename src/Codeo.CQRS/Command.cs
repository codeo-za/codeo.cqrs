using System;
using System.Collections.Generic;
using System.Transactions;
using Codeo.CQRS.Exceptions;

namespace Codeo.CQRS
{
    /// <summary>
    /// The base abstract class for all commands,
    /// inheriting from the BaseSqlExecutor, so it's
    /// ready for sql-based commands (though does not
    /// require sql-based logic at all).
    /// </summary>
    public abstract class Command : BaseSqlExecutor
    {
        private readonly List<Action<TransactionEventArgs>> _transactionCompletedHandlers =
            new List<Action<TransactionEventArgs>>();

        /// <summary>
        /// The query executor to use for sub-queries. If not set,
        /// this will be set by the default implementation when
        /// the CommandExecutor executes this command.
        /// </summary>
        public IQueryExecutor QueryExecutor { get; set; }

        /// <summary>
        /// The command executor to use for sub-commands. If not set,
        /// this will be set by the default implementation when
        /// the CommandExecutor executes this command.
        /// </summary>
        public ICommandExecutor CommandExecutor { get; set; }

        /// <summary>
        /// allows the caller to opt in to the current transaction's completion event
        /// </summary>
        public void OnTransactionCompleted(Action<TransactionEventArgs> handler)
        {
            lock (_transactionCompletedHandlers)
            {
                if (Transaction.Current == null)
                {
                    throw new InvalidOperationException("No ambient transaction scope exists");
                }

                _transactionCompletedHandlers.Add(handler);
                Transaction.Current.TransactionCompleted += (sender, args) => OnCommandTransactionComplete(args);
            }
        }

        /// <summary>
        /// The heart of the command logic.
        /// </summary>
        public abstract void Execute();

        /// <summary>
        /// Validates this instance.
        /// </summary>
        public virtual void Validate()
        {
            // not required to implement
        }

        /// <summary>
        /// Ensures that a transaction scope is available
        /// </summary>
        /// <exception cref="TransactionScopeRequired"></exception>
        public void ValidateTransactionScope()
        {
            if (Transaction.Current == null)
            {
                throw new TransactionScopeRequired(this);
            }
        }

        /// <summary>
        /// Fires when the command's transaction completes, or the Execute method fires when not within an ambient transaction
        /// </summary>
        private void OnCommandTransactionComplete(TransactionEventArgs args)
        {
            var handlers = new List<Action<TransactionEventArgs>>();
            lock (_transactionCompletedHandlers)
            {
                handlers.AddRange(_transactionCompletedHandlers);
                _transactionCompletedHandlers.Clear();
            }

            handlers.ForEach(handler =>
            {
                handler(args);
            });
        }
    }

    /// <summary>
    /// A typed command can return a single value of type T.
    /// Typically, this might be used for something like an
    /// insert where the id of the inserted item is returned.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Command<T> : Command
    {
        /// <summary>
        /// The result of this command's execution,
        /// when successful.
        /// </summary>
        public T Result { get; set; }
    }
}