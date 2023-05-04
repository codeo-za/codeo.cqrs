using System;
using System.Collections.Generic;
using Codeo.CQRS.Caching;

namespace Codeo.CQRS
{
    /// <summary>
    /// Executes queries
    /// </summary>
    public interface IQueryExecutor
    {
        /// <summary>
        /// Executes the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        void Execute(Query query);

        /// <summary>
        /// Executes the specified query.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        T Execute<T>(Query<T> query);

        /// <summary>
        /// Executes the specified queries.
        /// </summary>
        /// <param name="queries">The queries.</param>
        void Execute(IEnumerable<Query> queries);
    }

    /// <inheritdoc />
    public class QueryExecutor : IQueryExecutor
    {
        internal ICache CurrentCache => _cacheProvider?.Invoke();
        private readonly Func<ICache> _cacheProvider;

        /// <summary>
        /// Create a new query executor with the provided default
        /// cache implementation
        /// </summary>
        /// <param name="cache"></param>
        public QueryExecutor(
            ICache cache
        ) : this(() => cache)
        {
        }

        /// <summary>
        /// Create a new query executor with the provided default
        /// cache implementation provider
        /// </summary>
        /// <param name="cacheProvider"></param>
        public QueryExecutor(Func<ICache> cacheProvider)
        {
            _cacheProvider = cacheProvider;
        }

        /// <summary>
        /// Executes the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        public void Execute(Query query)
        {
            ExecuteWithNoResult(query);
        }

        /// <summary>
        /// Executes the specified query.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public T Execute<T>(Query<T> query)
        {
            ExecuteWithNoResult(query);
            return query.Result;
        }

        private void ExecuteWithNoResult(Query query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            query.QueryExecutor = this;
            query.Cache ??= _cacheProvider();
            query.Validate();
            query.Execute();
        }

        /// <summary>
        /// Executes the specified queries.
        /// </summary>
        /// <param name="queries">The queries.</param>
        public void Execute(IEnumerable<Query> queries)
        {
            if (queries == null)
            {
                throw new ArgumentNullException(nameof(queries));
            }

            foreach (var query in queries)
            {
                Execute(query);
            }
        }
    }
}