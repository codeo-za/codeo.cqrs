using System;

namespace Codeo.CQRS.Exceptions
{
    public class TransactionScopeRequired : Exception
    {
        public TransactionScopeRequired(Command command) :
            base($"Transaction scope is required when executing command '${command.GetType().Name}'")
        {
            Command = command;
        }

        public TransactionScopeRequired(Query query) :
            base($"Transaction scope is required when executing query '${query.GetType().Name}'")
        {
            Query = query;
        }

        public Command Command { get; set; }
        public Query Query { get; set; }
    }
}
