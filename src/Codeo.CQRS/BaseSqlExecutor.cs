using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Codeo.CQRS.Exceptions;
using Dapper;
using System.Collections.Concurrent;
using System.Reflection;
using Codeo.CQRS.Caching;

// ReSharper disable MemberCanBeProtected.Global

namespace Codeo.CQRS
{
    public enum CacheUsage
    {
        Full,
        WriteOnly,
        Bypass
    }

    public abstract class BaseSqlExecutor
    {
        internal static IDbConnectionFactory ConnectionFactory { get; set; }

        internal static void InstallExceptionHandler<T>(
            IExceptionHandler<T> handler) where T : Exception
        {
            var exType = typeof(T);
            lock (ExceptionHandlers)
            {
                if (!ExceptionHandlers.TryGetValue(exType, out _))
                {
                    ExceptionHandlers[exType] = new List<Tuple<IExceptionHandler, Func<Operation, Exception, bool>>>();
                }

                ExceptionHandlers[exType].Add(
                    Tuple.Create<IExceptionHandler, Func<Operation, Exception, bool>>(
                        handler,
                        (op, ex) => handler.Handle(op, ex as T)
                    )
                );
                ExceptionHandlerCache.TryRemove(exType, out _);
            }
        }

        internal static void UninstallExceptionHandler<T>(
            IExceptionHandler<T> handler) where T : Exception
        {
            var exType = typeof(T);
            lock (ExceptionHandlers)
            {
                if (!ExceptionHandlers.TryGetValue(exType, out var collection))
                {
                    return;
                }

                collection.RemoveAll(o => o.Item1 == handler);
                ExceptionHandlerCache.TryRemove(exType, out _);
            }
        }

        internal static readonly Dictionary<Type, List<Tuple<IExceptionHandler, Func<Operation, Exception, bool>>>>
            ExceptionHandlers
                = new Dictionary<Type, List<Tuple<IExceptionHandler, Func<Operation, Exception, bool>>>>();

        internal static readonly ConcurrentDictionary<Type, Func<Operation, Exception, bool>[]>
            ExceptionHandlerCache = new ConcurrentDictionary<Type, Func<Operation, Exception, bool>[]>();

        public ICache Cache { get; set; }
        public CacheUsage CacheUsage { get; set; } = CacheUsage.Full;

        protected void InvalidateCache()
        {
            var cacheKey = GenerateCacheKey();
            Cache.Remove(cacheKey);
        }

        private T Through<T>(Func<T> generator)
        {
            switch (CacheUsage)
            {
                case CacheUsage.Bypass:
                    return generator();
                case CacheUsage.WriteOnly:
                {
                    var result = generator();
                    return CacheResultIfRequired(result);
                }
                default:
                    var cacheOptions = GenerateCacheOptions();
                    var cacheKey = GenerateCacheKey();
                    if (!cacheOptions.Enabled)
                    {
                        // inheriting class may override GenerateCacheOptions
                        // to specifically return no-cache
                        return generator();
                    }

                    return cacheOptions.AbsoluteExpiration.HasValue
                        ? Cache.GetOrSet(cacheKey, generator, cacheOptions.AbsoluteExpiration.Value)
                        // the Enabled property double-checks this
                        // ReSharper disable once PossibleInvalidOperationException
                        : Cache.GetOrSet(cacheKey, generator, cacheOptions.SlidingExpiration.Value);
            }
        }

        protected T CacheResultIfRequired<T>(
            T result)
        {
            var cacheItemExpiration = GenerateCacheOptions();
            if (!cacheItemExpiration.Enabled)
            {
                return result;
            }

            var cacheKey = GenerateCacheKey();
            if (cacheItemExpiration.AbsoluteExpiration.HasValue)
            {
                Cache.Set(
                    cacheKey,
                    result,
                    cacheItemExpiration.AbsoluteExpiration.Value
                );
            }
            else if (cacheItemExpiration.SlidingExpiration.HasValue)
            {
                Cache.Set(
                    cacheKey,
                    cacheItemExpiration.SlidingExpiration.Value
                );
            }
            else
            {
                Cache.Set(
                    cacheKey,
                    result
                );
            }

            return result;
        }

        protected class CacheExpiration
        {
            public DateTime? AbsoluteExpiration { get; }
            public TimeSpan? SlidingExpiration { get; }

            public bool Enabled =>
                _enabled &&
                (AbsoluteExpiration.HasValue ||
                    SlidingExpiration.HasValue);

            private readonly bool _enabled;


            public CacheExpiration(bool enabled)
            {
                _enabled = enabled;
            }

            public CacheExpiration(TimeSpan slidingExpiration) : this(true)
            {
                SlidingExpiration = slidingExpiration;
            }

            public CacheExpiration(DateTime absoluteExpiration) : this(true)
            {
                AbsoluteExpiration = absoluteExpiration;
            }
        }

        protected virtual CacheExpiration GenerateCacheOptions()
        {
            if (MyCacheAttribute == null)
            {
                return new CacheExpiration(false);
            }

            return MyCacheAttribute.CacheExpiration == CQRS.CacheExpiration.Absolute
                ? new CacheExpiration(DateTime.Now.AddSeconds(MyCacheAttribute.TTL))
                : new CacheExpiration(TimeSpan.FromSeconds(MyCacheAttribute.TTL));
        }

        private CacheAttribute MyCacheAttribute =>
            _myCacheAttribute ??= FindMyCacheAttribute();

        private CacheAttribute _myCacheAttribute;

        private Type MyType =>
            _myType ??= GetType();

        private Type _myType;

        private CacheAttribute FindMyCacheAttribute()
        {
            return MyType
                .GetCustomAttributes(true)
                .OfType<CacheAttribute>()
                .FirstOrDefault();
        }

        /// <summary>
        /// override this if the default cache key generation is not
        /// sufficient for your needs
        /// </summary>
        /// <returns></returns>
        protected virtual string GenerateCacheKey()
        {
            if (MyCacheAttribute == null)
            {
                return GetType().Name;
            }

            var parts = CacheProps.Aggregate(
                new List<string>() { MyType.Name },
                (acc, cur) =>
                {
                    acc.Add(PropertyKeyFor(cur));
                    return acc;
                });
            return string.Join(
                "-",
                parts
            );
        }

        private string PropertyKeyFor(PropertyInfo cur)
        {
            return $"{cur.Name}:{cur.GetValue(this)}";
        }

        private PropertyInfo[] CacheProps =>
            _cacheProps ??= FindCacheProps();

        private PropertyInfo[] _cacheProps;

        private PropertyInfo[] FindCacheProps()
        {
            var result = MyType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(pi => MyCacheAttribute.CacheKeyProperties.Contains(pi.Name))
                .ToArray();
            var missing = MyCacheAttribute.CacheKeyProperties
                .Except(result.Select(pi => pi.Name))
                .ToArray();
            if (missing.Any())
            {
                throw new InvalidCachePropertiesSpecified(missing);
            }

            return result;
        }

        private void SetCache<T>(T result)
        {
            Cache.Set(GenerateCacheKey(), result);
        }

        internal static readonly ConcurrentDictionary<Type, bool> KnownMappedTypes
            = new ConcurrentDictionary<Type, bool>();


        /// <summary>
        /// Selects zero or more items from the database
        /// </summary>
        /// <param name="sql"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerable<T> SelectMany<T>(string sql)
        {
            return SelectMany<T>(sql, null);
        }

        /// <summary>
        /// Selects zero or more items from the database
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerable<T> SelectMany<T>(
            string sql,
            object parameters)
        {
            return Through(
                () => QueryCollection<T>(Operation.Select, sql, parameters)
                    ??
                    // there are many usages of Query<T> where the result
                    //    isn't checked, but is immediately chained into LINQ,
                    //    which simply fails with a NullReferenceException. Better
                    //    to catch it here.
                    throw new InvalidOperationException(
                        $"{GetType()}: QueryExecutor<T> where T is IEnumerable<> should return empty collection rather than null."
                    )
            );
        }

        /// <summary>
        /// Selects the first matching item from the database
        /// </summary>
        /// <param name="sql"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T SelectFirst<T>(
            string sql)
        {
            return SelectFirst<T>(sql, null);
        }

        /// <summary>
        /// Selects the first matching item from the database
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T SelectFirst<T>(
            string sql,
            object parameters)
        {
            return Through(
                () => QueryFirst<T>(
                    Operation.Select,
                    sql,
                    parameters)
            );
        }

        /// <summary>
        /// Selects multiple results from a horizontally joined query result. (2 Types)
        /// </summary>
        /// <typeparam name="TFirst">The type of the first result object.</typeparam>
        /// <typeparam name="TSecond">The type of the second result object.</typeparam>
        /// <typeparam name="TReturn">The type of the returned result object.</typeparam>
        /// <param name="sql">The SQL query</param>
        /// <param name="function">The function on how to handle the returned results</param>
        /// <returns></returns>
        public IEnumerable<TReturn> SelectMulti<TFirst, TSecond, TReturn>(
            string sql,
            Func<TFirst, TSecond, TReturn> function)

        {
            return SelectMulti<TFirst, TSecond, TReturn>(
                sql,
                function,
                null);
        }

        /// <summary>
        /// Selects multiple results from a horizontally joined query result. (2 Types)
        /// </summary>
        /// <typeparam name="TFirst">The type of the first result object.</typeparam>
        /// <typeparam name="TSecond">The type of the second result object.</typeparam>
        /// <typeparam name="TReturn">The type of the returned result object.</typeparam>
        /// <param name="sql">The SQL query</param>
        /// <param name="function">The function on how to handle the returned results</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        public IEnumerable<TReturn> SelectMulti<TFirst, TSecond, TReturn>(
            string sql,
            Func<TFirst, TSecond, TReturn> function,
            object parameters)
        {
            return Through(() =>
            {
                EnsureDapperKnowsAbout<TFirst>();
                EnsureDapperKnowsAbout<TSecond>();
                using var connection = CreateOpenConnection();

                List<TReturn> Execute(IDbConnection conn)
                {
                    return conn.Query(sql, function, parameters).ToList();
                }

                return SelectRowsOnConnection(connection, Execute);
            });
        }

        private IDbConnection CreateOpenConnection()
        {
            var result = ConnectionFactory?.Create()
                ?? throw new InvalidOperationException(
                    "Please configure a ConnectionFactory to provide new instances of IDbConnection per call to Create()");
            ;
            if (result.State != ConnectionState.Open)
            {
                result.Open();
            }

            return result;
        }

        /// <summary>
        /// Selects multiple results from a horizontally joined query result. (3 Types)
        /// </summary>
        /// <typeparam name="TFirst">The type of the first result object.</typeparam>
        /// <typeparam name="TSecond">The type of the second result object.</typeparam>
        /// <typeparam name="TThird">The type of the third result object.</typeparam>
        /// <typeparam name="TReturn">The type of the returned result object.</typeparam>
        /// <param name="sql">The SQL query</param>
        /// <param name="function">The function on how to handle the returned results</param>
        public IEnumerable<TReturn> SelectMulti<TFirst, TSecond, TThird, TReturn>(
            string sql,
            Func<TFirst, TSecond, TThird, TReturn> function)
        {
            return SelectMulti<TFirst, TSecond, TThird, TReturn>(
                sql,
                function,
                null);
        }

        /// <summary>
        /// Selects multiple results from a horizontally joined query result. (3 Types)
        /// </summary>
        /// <typeparam name="TFirst">The type of the first result object.</typeparam>
        /// <typeparam name="TSecond">The type of the second result object.</typeparam>
        /// <typeparam name="TThird">The type of the third result object.</typeparam>
        /// <typeparam name="TReturn">The type of the returned result object.</typeparam>
        /// <param name="sql">The SQL query</param>
        /// <param name="function">The function on how to handle the returned results</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        public IEnumerable<TReturn> SelectMulti<TFirst, TSecond, TThird, TReturn>(
            string sql,
            Func<TFirst, TSecond, TThird, TReturn> function,
            object parameters)
        {
            return Through(() =>
            {
                EnsureDapperKnowsAbout<TFirst>();
                EnsureDapperKnowsAbout<TSecond>();
                EnsureDapperKnowsAbout<TThird>();
                using var connection = CreateOpenConnection();

                List<TReturn> Execute(IDbConnection conn)
                {
                    return conn.Query(sql, function, parameters).ToList();
                }

                return SelectRowsOnConnection(
                    connection,
                    Execute
                );
            });
        }

        private List<TReturnItem> SelectRowsOnConnection<TReturnItem>(
            IDbConnection connection,
            Func<IDbConnection, List<TReturnItem>> executor
        )
        {
            try
            {
                return executor(connection);
            }
            catch (Exception ex)
            {
                if (!TryHandleException(Operation.Select, ex))
                {
                    throw;
                }

                return default;
            }
        }

        private Func<Operation, Exception, bool>[] FindHandlersFor(Exception ex)
        {
            var exType = ex.GetType();
            if (ExceptionHandlerCache.TryGetValue(exType, out var cached))
            {
                return cached;
            }

            Func<Operation, Exception, bool>[] result = null;
            lock (ExceptionHandlers)
            {
                if (ExceptionHandlers.TryGetValue(ex.GetType(), out var handlers))
                {
                    result = handlers
                        .Select(h => h.Item2)
                        .ToArray();
                }
            }

            result ??= new Func<Operation, Exception, bool>[0];
            ExceptionHandlerCache.TryAdd(exType, result);

            // TODO: try to find derived handler? probably travel all the way up to Exception -- would be useful
            // for the caller to be able to install a generic Exception handler
            return result;
        }


        private bool TryHandleException(Operation operation, Exception ex)
        {
            var handlers = FindHandlersFor(ex);
            foreach (var handler in handlers)
            {
                if (handler.Invoke(operation, ex))
                {
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Selects multiple results from a vertically joined query result. (multiple select statements)
        /// </summary>
        /// <param name="sql">The SQL query.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="callback">The callback action on how to handle the results.</param>
        public void SelectMulti(
            string sql,
            object parameters,
            Action<SqlMapper.GridReader> callback)
        {
            using var connection = CreateOpenConnection();
            SelectMultiOnConnection(
                connection,
                sql,
                parameters,
                callback
            );
        }

        private void SelectMultiOnConnection(
            IDbConnection connection,
            string sql,
            object parameters,
            Action<SqlMapper.GridReader> callback
        )
        {
            try
            {
                callback(
                    Through(() => connection.QueryMultiple(sql, parameters))
                );
            }
            catch (Exception ex)
            {
                if (TryHandleException(Operation.Select, ex))
                {
                    return;
                }

                throw;
            }
        }

        /// <summary>
        /// Performs an update, returns a collection from the result
        /// </summary>
        /// <param name="sql"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerable<T> UpdateGetList<T>(
            string sql)
        {
            return UpdateGetList<T>(sql, null);
        }

        /// <summary>
        /// Performs an update, returns a collection from the result
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerable<T> UpdateGetList<T>(
            string sql,
            object parameters)
        {
            return QueryCollection<T>(Operation.Update, sql, parameters);
        }

        public T UpdateGetFirst<T>(string sql)
        {
            return UpdateGetFirst<T>(sql, null);
        }

        public T UpdateGetFirst<T>(string sql, object parameters)
        {
            return Through(
                () => QueryFirst<T>(Operation.Update, sql, parameters)
            );
        }

        public IEnumerable<T> InsertGetList<T>(string sql)
        {
            return InsertGetList<T>(sql, null);
        }

        public IEnumerable<T> InsertGetList<T>(string sql, object parameters)
        {
            return QueryCollection<T>(Operation.Insert, sql, parameters);
        }

        public T InsertGetFirst<T>(string sql)
        {
            return InsertGetFirst<T>(sql, null);
        }

        public T InsertGetFirst<T>(string sql, object parameters)
        {
            return QueryFirst<T>(Operation.Insert, sql, parameters);
        }

        public IEnumerable<T> DeleteGetList<T>(string sql)
        {
            return DeleteGetList<T>(sql, null);
        }

        public IEnumerable<T> DeleteGetList<T>(string sql, object parameters)
        {
            return QueryCollection<T>(Operation.Delete, sql, parameters);
        }

        public T DeleteGetFirst<T>(string sql)
        {
            return DeleteGetFirst<T>(sql, null);
        }

        public T DeleteGetFirst<T>(string sql, object parameters)
        {
            return QueryFirst<T>(Operation.Delete, sql, parameters);
        }


        public int ExecuteUpdate(string sql)
        {
            return ExecuteUpdate(sql, null);
        }

        public int ExecuteUpdate(string sql, object parameters)
        {
            return Execute(Operation.Update, sql, parameters);
        }

        public int ExecuteInsert(string sql)
        {
            return ExecuteInsert(sql, null);
        }

        public int ExecuteInsert(string sql, object parameters)
        {
            return Execute(Operation.Insert, sql, parameters);
        }

        public int ExecuteDelete(string sql)
        {
            return ExecuteDelete(sql, null);
        }

        public int ExecuteDelete(string sql, object parameters)
        {
            return Execute(Operation.Delete, sql, parameters);
        }

        /// <summary>
        /// Executes a query returning a list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="operation"></param>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private IEnumerable<T> QueryCollection<T>(
            Operation operation,
            string sql,
            object parameters)
        {
            EnsureDapperKnowsAbout<T>();
            using var connection = CreateOpenConnection();
            return RunListResultOnConnection<T>(
                operation,
                connection,
                sql,
                parameters
            );
        }

        private IEnumerable<T> RunListResultOnConnection<T>(
            Operation operation,
            IDbConnection connection,
            string sql,
            object parameters
        )
        {
            try
            {
                return connection.Query<T>(sql, parameters);
            }
            catch (Exception ex)
            {
                if (!TryHandleException(operation, ex))
                {
                    throw;
                }

                return new List<T>();
            }
        }

        private T QueryFirst<T>(
            Operation operation,
            string sql,
            object parameters)
        {
            EnsureDapperKnowsAbout<T>();
            using var connection = CreateOpenConnection();
            return RunSingleResultQueryOnConnection(
                operation,
                connection,
                conn =>
                {
                    try
                    {
                        return conn.QueryFirst<T>(sql, parameters);
                    }
                    catch (Exception ex)
                    {
                        if (LooksLikeNoRowsReturned(ex))
                        {
                            throw new EntityDoesNotExistException(
                                typeof(T).Name,
                                parameters,
                                ex
                            );
                        }

                        throw;
                    }
                });
        }

        private bool LooksLikeNoRowsReturned(Exception ex)
        {
            return ex is InvalidOperationException &&
                ex.StackTrace.Split('\n')
                    .Any(s => s.Contains(EnumerableFirst));
        }

        private const string EnumerableFirst =
            nameof(Enumerable) + "." + nameof(System.Linq.Enumerable.First);

        private T RunSingleResultQueryOnConnection<T>(
            Operation operation,
            IDbConnection connection,
            Func<IDbConnection, T> queryExecutor)
        {
            try
            {
                return queryExecutor(connection);
            }
            catch (Exception ex)
            {
                if (!TryHandleException(operation, ex))
                {
                    throw;
                }

                return default;
            }
        }

        protected int Execute(
            Operation operation,
            string sql,
            object parameters,
            int? commandTimeout = null)
        {
            return Execute(
                operation,
                sql,
                parameters,
                commandTimeout,
                NoOp
            );
        }

        protected int Execute<TException>(
            Operation operation,
            string sql,
            object parameters,
            int? commandTimeout = null,
            IExceptionHandler<TException> customExceptionHandler = null
        ) where TException : Exception
        {
            using var connection = CreateOpenConnection();
            return ExecuteOnConnection(
                connection,
                operation,
                sql,
                parameters,
                commandTimeout,
                customExceptionHandler
            );
        }

        private static readonly IExceptionHandler<Exception> NoOp = null;

        private int ExecuteOnConnection<TException>(
            IDbConnection connection,
            Operation operation,
            string sql,
            object parameters,
            int? commandTimeout = null,
            IExceptionHandler<TException> customExceptionHandler = null
        ) where TException : Exception
        {
            try
            {
                return connection.Execute(sql, parameters, commandTimeout: commandTimeout);
            }
            catch (Exception ex)
            {
                if (ex is TException handledException)
                {
                    if (customExceptionHandler?.Handle(operation, handledException) ?? false)
                    {
                        return default;
                    }
                }

                if (!TryHandleException(operation, ex))
                {
                    throw;
                }

                return default;
            }
        }

        private void EnsureDapperKnowsAbout<T>()
        {
            var type = typeof(T);
            type = type.GetCollectionItemType() ?? type;
            if (KnownMappedTypes.ContainsKey(type))
            {
                return;
            }

            Fluently.Configuration.MapEntityType(type);
        }

        public static void RemoveAllExceptionHandlers()
        {
            ExceptionHandlers.Clear();
        }
    }
}