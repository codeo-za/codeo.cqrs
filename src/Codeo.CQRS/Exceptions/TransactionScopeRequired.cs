using System;

namespace Codeo.CQRS.Exceptions
{
    /// <summary>
    /// Thrown when a command or query requires a
    /// transaction scope and has called VerifyTransactionScope.
    /// Transactions should be created, committed or rolled-back
    /// outside of command / query space.
    /// </summary>
    public class TransactionScopeRequired : Exception
    {
        /// <summary>
        /// Creates the exception for any executor
        /// </summary>
        /// <param name="executor"></param>
        public TransactionScopeRequired(
            IExecutor executor
        ) : base(GenerateMessageFor(executor))
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="command"></param>
        public TransactionScopeRequired(
            ICommand command
        ) : this(command as IExecutor)
        {
            Command = command;
        }

        /// <inheritdoc />
        public TransactionScopeRequired(
            IQuery query
        ) : this(query as IExecutor)
        {
            Query = query;
        }

        private static string GenerateMessageFor(IExecutor executor)
        {
            return executor switch
            {
                ICommand => GenerateMessage("command"),
                IQuery => GenerateMessage("query"),
                _ => "Transaction scope is required"
            };

            string GenerateMessage(string label)
            {
                return $"Transaction scope is required when executing {label} '${executor.GetType().Name}'";
            }
        }

        /// <summary>
        /// The command that required the transaction, if supplied
        /// </summary>
        public ICommand Command { get; private set; }

        /// <summary>
        /// The query that required the transaction, if supplied
        /// </summary>
        public IQuery Query { get; private set; }
    }
}