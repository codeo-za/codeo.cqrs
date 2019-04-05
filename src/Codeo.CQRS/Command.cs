using System;
using System.Collections.Generic;
using System.Transactions;
using Codeo.CQRS.MySql.Exceptions;

namespace Codeo.CQRS.MySql
{
    public abstract class Command : BaseSqlExecutor
    {
        private List<Action<TransactionEventArgs>> _transactionCompletedHandlers =
            new List<Action<TransactionEventArgs>>();

        public IQueryExecutor QueryExecutor { get; set; }
        public ICommandExecutor CommandExecutor { get; set; }

        /// <summary>
        /// allows the caller to opt in to the current transaction's completion event
        /// </summary>
        protected void NotifyOnTransactionCompleted(Action<TransactionEventArgs> handler)
        {
            lock (_transactionCompletedHandlers)
            {
                if (Transaction.Current != null)
                {
                    _transactionCompletedHandlers.Add(handler);
                    Transaction.Current.TransactionCompleted += (sender, args) => OnCommandTransactionComplete(args);
                }
                else
                {
                    handler(new TransactionEventArgs());
                }
            }
        }
        
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
        /// <exception cref="TransactionScopeRequiredException"></exception>
        public void ValidateTransactionScope()
        {
            if (Transaction.Current == null)
            {
                throw new TransactionScopeRequiredException(this);
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

    public abstract class Command<T> : Command
    {
        public T Result { get; set; }
    }
}