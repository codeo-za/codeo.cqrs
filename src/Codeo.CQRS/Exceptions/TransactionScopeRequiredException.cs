using System;

namespace Codeo.CQRS.MySql.Exceptions
{
    public class TransactionScopeRequiredException : Exception
    {
        public TransactionScopeRequiredException(Command command) :
            base($"Transaction scope is required when executing command '${command.GetType().Name}'")
        {
            Command = command;
        }

        public TransactionScopeRequiredException(Query query) :
            base($"Transaction scope is required when executing query '${query.GetType().Name}'")
        {
            Query = query;
        }

        public Command Command { get; set; }
        public Query Query { get; set; }
    }
}
