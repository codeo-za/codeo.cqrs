using System;
using System.Runtime.Serialization;

namespace Codeo.CQRS.Exceptions
{
    [Serializable]
    public class TransactionScopeRequired : Exception
    {
        protected TransactionScopeRequired(SerializationInfo info, StreamingContext streamingContext) : base(info,
            streamingContext)
        {
            // for serializable logging support.  
        }
        
        public TransactionScopeRequired(Command command) :
            base($"Transaction scope is required when executing command '${command.GetType().Name}'")
        {
            Command = command;
        }
        
        public TransactionScopeRequired(CommandAsync command) :
            base($"Transaction scope is required when executing command '${command.GetType().Name}'")
        {
            CommandAsync = command;
        }

        public TransactionScopeRequired(Query query) :
            base($"Transaction scope is required when executing query '${query.GetType().Name}'")
        {
            Query = query;
        }
        
        public TransactionScopeRequired(QueryAsync query) :
            base($"Transaction scope is required when executing query '${query.GetType().Name}'")
        {
            QueryAsync = query;
        }

        public Command? Command { get; set; }
        public CommandAsync? CommandAsync { get; set; }
        public Query? Query { get; set; }
        public QueryAsync? QueryAsync { get; set; }
    }
}
 