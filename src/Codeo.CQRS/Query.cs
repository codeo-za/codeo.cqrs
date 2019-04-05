using System;
using System.Transactions;
using Codeo.CQRS.MySql.Exceptions;
using Newtonsoft.Json;

namespace Codeo.CQRS.MySql
{
    public abstract class Query : BaseSqlExecutor
    {
        internal static Func<IQueryExecutor> QueryExecutorFactory = () => new QueryExecutor();

        [JsonIgnore]
        public IQueryExecutor QueryExecutor { get; set; } = QueryExecutorFactory();

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
                throw new TransactionScopeRequiredException(this);
            }
        }
    }

    public abstract class Query<T> : Query
    {
        public T Result { get; set; }
    }
}
