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
        /// 
        /// </summary>
        /// <param name="command"></param>
        public TransactionScopeRequired(Command command) :
            base($"Transaction scope is required when executing command '${command.GetType().Name}'")
        {
            Command = command;
        }

        /// <inheritdoc />
        public TransactionScopeRequired(IQuery query) :
            base($"Transaction scope is required when executing query '${query.GetType().Name}'")
        {
            Query = query;
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
