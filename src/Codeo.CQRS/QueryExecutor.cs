using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Codeo.CQRS.Caching;

namespace Codeo.CQRS
{
    public interface IQueryExecutor
    {
        /// <summary>
        /// Executes the specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        void Execute(Query query);

        /// <summary>
        /// Executes the specified query asynchronously.
        /// </summary>
        /// <param name="query">The query.</param>
        Task ExecuteAsync(QueryAsync query);

        /// <summary>
        /// Executes the specified query.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        T? Execute<T>(Query<T> query);


        /// <summary>
        /// Executes the specified query asynchronously.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queryAsync">The query.</param>
        /// <returns></returns>
        Task<T?> ExecuteAsync<T>(QueryAsync<T> queryAsync);

        /// <summary>
        /// Executes the specified queries.
        /// </summary>
        /// <param name="queries">The queries.</param>
        void Execute(IEnumerable<Query> queries);
    }

    public class QueryExecutor : IQueryExecutor
    {
        private readonly ICache _cache;
        public QueryExecutor(ICache cache)
        {
            _cache = cache;
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
        /// <param name="query">The query.</param>
        public async Task ExecuteAsync(QueryAsync query)
        {
            await ExecuteWithNoResultAsync(query);
        }

        /// <summary>
        /// Executes the specified query.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="query">The query.</param>
        /// <returns></returns>
        public T? Execute<T>(Query<T> query)
        {
            ExecuteWithNoResult(query);
            return query.Result;
        }

        /// <summary>
        /// Executes the specified query.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="queryAsync">The query.</param>
        /// <returns></returns>
        public async Task<T?> ExecuteAsync<T>(QueryAsync<T> queryAsync)
        {
            await ExecuteWithNoResultAsync(queryAsync);
            return queryAsync.Result;
        }

        private void ExecuteWithNoResult(Query query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            query.QueryExecutor = this;
            query.Cache ??= _cache;
            query.Validate();
            query.Execute();
        }

        private async Task ExecuteWithNoResultAsync(QueryAsync query)
        {
            if (query == null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            query.QueryExecutor = this;
            query.Cache ??= _cache;
            query.Validate();
            await query.ExecuteAsync();
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