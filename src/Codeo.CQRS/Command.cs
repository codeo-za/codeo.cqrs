using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Transactions;
using Codeo.CQRS.Exceptions;

namespace Codeo.CQRS
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
        
        public abstract void Execute();

        /// <summary>
        /// Validates this instance.
        /// </summary>
        public abstract void Validate();

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

    public abstract class Command<T> : Command
    {
        public T Result { get; set; } = default!;
    }

    public abstract class CommandAsync : BaseSqlExecutor
    {
        private List<Action<TransactionEventArgs>> _transactionCompletedHandlers =
            new List<Action<TransactionEventArgs>>();
        public IQueryExecutor? QueryExecutor { get; set; }
        public ICommandExecutor? CommandExecutor { get; set; }

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
        
        public abstract Task ExecuteAsync();

        /// <summary>
        /// Validates this instance.
        /// </summary>
        public abstract void Validate();

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

    public abstract class CommandAsync<T> : CommandAsync
    {
        public T Result { get; set; } = default!;
    }
}