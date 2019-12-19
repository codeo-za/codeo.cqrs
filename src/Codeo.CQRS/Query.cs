using System.Transactions;
using Codeo.CQRS.Exceptions;
using Newtonsoft.Json;

namespace Codeo.CQRS
{
    public abstract class Query : BaseSqlExecutor
    {
        [JsonIgnore]
        public IQueryExecutor QueryExecutor { get; set; }

        /// <summary>
        /// Executes this instance.
        /// </summary>
        public abstract void Execute();

        /// <summary>
        /// Validates this instance.
        /// </summary>
        public virtual void Validate()
        {
            // not required to override this
        }

        public void ValidateTransactionScope()
        {
            if (Transaction.Current == null)
            {
                throw new TransactionScopeRequired(this);
            }
        }
    }

    public abstract class Query<T> : Query
    {
        public T Result { get; set; }
    }
}
