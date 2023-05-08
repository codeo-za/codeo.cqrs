using System;
using System.Collections.Generic;
using System.Transactions;
using Codeo.CQRS.Caching;
using Codeo.CQRS.Exceptions;

namespace Codeo.CQRS
{
    /// <summary>
    /// The base contract for a command
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// The query executor to use for sub-queries. If not set,
        /// this will be set by the default implementation when
        /// the CommandExecutor executes this command.
        /// </summary>
        IQueryExecutor QueryExecutor { get; set; }

        /// <summary>
        /// The command executor to use for sub-commands. If not set,
        /// this will be set by the default implementation when
        /// the CommandExecutor executes this command.
        /// </summary>
        ICommandExecutor CommandExecutor { get; set; }

        /// <summary>
        /// The cache implementation to use for sub-queries
        /// </summary>
        ICache Cache { get; set; }
        
        /// <summary>
        /// Executes the command's logic
        /// </summary>
        void Execute();
        
        /// <summary>
        /// Validates the command's state before
        /// execution
        /// </summary>
        void Validate();

        /// <summary>
        /// allows the caller to opt in to the current transaction's completion event
        /// </summary>
        void OnTransactionCompleted(Action<TransactionEventArgs> handler);
    }

    /// <summary>
    /// The contract for a command which returns a result
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ICommand<out T>: ICommand
    {
        /// <summary>
        /// The result returned by the command
        /// </summary>
        T Result { get; }
    }

    /// <inheritdoc cref="Codeo.CQRS.ICommand" />
    public abstract class Command : BaseSqlExecutor, ICommand
    {
        private readonly List<Action<TransactionEventArgs>> _transactionCompletedHandlers =
            new List<Action<TransactionEventArgs>>();

        /// <inheritdoc />
        public IQueryExecutor QueryExecutor { get; set; }

        /// <inheritdoc />
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
                Transaction.Current.TransactionCompleted += (_, args) => OnCommandTransactionComplete(args);
            }
        }

        /// <inheritdoc />
        public abstract void Execute();

        /// <inheritdoc />
        public abstract void Validate();

        /// <summary>
        /// Ensures that a transaction scope is available
        /// </summary>
        /// <exception cref="TransactionScopeRequired"></exception>
        protected void ValidateTransactionScope()
        {
            if (Transaction.Current == null)
            {
                throw new TransactionScopeRequired(this);
            }
        }

        /// <summary>
        /// Executes the sub-command and returns the result
        /// </summary>
        /// <param name="command"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected T Execute<T>(ICommand<T> command)
        {
            return CommandExecutor.Execute(command);
        }

        /// <summary>
        /// Executes the sub-command
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        protected void Execute(ICommand command)
        {
            CommandExecutor.Execute(command);
        }

        /// <summary>
        /// Executes the sub-query and returns the result
        /// </summary>
        /// <param name="query"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected T Execute<T>(IQuery<T> query)
        {
            return QueryExecutor.Execute(query);
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

    /// <inheritdoc cref="Codeo.CQRS.ICommand" />
    public abstract class Command<T> : Command, ICommand<T>
    {
        /// <inheritdoc />
        public T Result { get; protected set; }
    }
}