using System.Transactions;
using Codeo.CQRS.Caching;
using Codeo.CQRS.Exceptions;
using Newtonsoft.Json;

namespace Codeo.CQRS
{
    /// <summary>
    /// Forms the shared contract for ICommand and IQuery
    /// </summary>
    public interface IExecutor
    {
        /// <summary>
        /// The cache implementation to use for sub-queries
        /// </summary>
        ICache Cache { get; set; }

        /// <summary>
        /// Provided query executor for sub-queries
        /// </summary>
        IQueryExecutor QueryExecutor { get; set; }

        /// <summary>
        /// Executes the query - the result should be stored
        /// in Result
        /// </summary>
        void Execute();

        /// <summary>
        /// Performs validation for this query
        /// </summary>
        void Validate();
    }

    /// <summary>
    /// The base contract for a query - useful as a collection type
    /// </summary>
    public interface IQuery : IExecutor
    {
    }

    /// <summary>
    /// The contract for a query, describing what it should do
    /// and provide
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IQuery<out T> : IQuery
    {
        /// <summary>
        /// The final result of the query (should be returned
        /// by the QueryExecutor.Execute method)
        /// </summary>
        T Result { get; }
    }

    /// <summary>
    /// The base type for queries returning a value (single
    /// value or collection). The result of the query on
    /// success should be stored in the Result property.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Query<T> : BaseSqlExecutor, IQuery<T>
    {
        /// <summary>
        /// The result of the successful query
        /// </summary>
        public T Result { get; protected set; }

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

        /// <summary>
        /// Executes the provided query and returns the result
        /// </summary>
        /// <param name="query"></param>
        /// <typeparam name="TSub"></typeparam>
        /// <returns></returns>
        protected TSub Execute<TSub>(IQuery<TSub> query)
        {
            return QueryExecutor.Execute(query);
        }
    }
}