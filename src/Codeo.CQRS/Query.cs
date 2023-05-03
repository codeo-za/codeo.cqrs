using System.Transactions;
using Codeo.CQRS.Exceptions;
using Newtonsoft.Json;

namespace Codeo.CQRS
{
    /// <summary>
    /// Provides the base query class. Not particularly useful
    /// on its own because it has no return value, but useful
    /// as a collection type.
    /// </summary>
    public abstract class Query : BaseSqlExecutor
    {
        /// <summary>
        /// The (optional) query executor to use for sub-queries.
        /// If not set, it will be set by the outer QueryExecutor.
        /// </summary>
        [JsonIgnore]
        public IQueryExecutor QueryExecutor { get; set; }

        /// <summary>
        /// Executes this instance.
        /// </summary>
        public abstract void Execute();

        /// <summary>
        /// Validates this instance.
        /// </summary>
        public abstract void Validate();

        /// <summary>
        /// Validates that there currently is an active transaction
        /// </summary>
        /// <exception cref="TransactionScopeRequired"></exception>
        public void ValidateTransactionScope()
        {
            if (Transaction.Current == null)
            {
                throw new TransactionScopeRequired(this);
            }
        }
    }

    /// <summary>
    /// The base type for queries returning a value (single
    /// value or collection). The result of the query on
    /// success should be stored in the Result property.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Query<T> : Query
    {
        /// <summary>
        /// The result of the successful query
        /// </summary>
        public T Result { get; set; }
    }
}
