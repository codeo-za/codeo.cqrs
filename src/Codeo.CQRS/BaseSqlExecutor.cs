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
    /// <summary>
    /// Different strategies for cache usage
    /// </summary>
    public enum CacheUsage
    {
        /// <summary>
        /// Read from the cache if available, write to the cache
        /// on cache miss
        /// </summary>
        Full,

        /// <summary>
        /// Read fresh, but write to the cache
        /// </summary>
        WriteOnly,

        /// <summary>
        /// Completely bypass cache - no read or write
        /// </summary>
        Bypass
    }

    /// <summary>
    /// The base class for commands and queries, with protected methods
    /// likely to be employed by either in the scenarios of sql relational
    /// database queries &amp; commands
    /// </summary>
    public abstract class BaseSqlExecutor
    {
        /// <summary>
        /// Default column to split Multi&lt;&gt; query results on.
        /// see: https://github.com/StackExchange/Dapper#multi-mapping
        /// </summary>
        public const string DEFAULT_SPLIT_ON_COLUMN = "Id";

        internal static IDbConnectionFactory ConnectionFactory { get; set; }

        internal static void InstallExceptionHandler<T>(
            IExceptionHandler<T> handler) where T : Exception
        {
            var exType = typeof(T);
            lock (ExceptionHandlers)
            {
                if (!ExceptionHandlers.TryGetValue(exType, out _))
                {
                    ExceptionHandlers[exType] =
                        new List<Tuple<IExceptionHandler, Func<Operation, Exception, ExceptionHandlingStrategy>>>();
                }

                ExceptionHandlers[exType].Add(
                    Tuple.Create<IExceptionHandler, Func<Operation, Exception, ExceptionHandlingStrategy>>(
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
                ExceptionHandlers[exType] = collection;
                ExceptionHandlerCache.TryRemove(exType, out _);
            }
        }

        internal static readonly Dictionary<Type,
                List<Tuple<IExceptionHandler, Func<Operation, Exception, ExceptionHandlingStrategy>>>>
            ExceptionHandlers
                = new Dictionary<Type,
                    List<Tuple<IExceptionHandler, Func<Operation, Exception, ExceptionHandlingStrategy>>>>();

        internal static readonly ConcurrentDictionary<Type, Func<Operation, Exception, ExceptionHandlingStrategy>[]>
            ExceptionHandlerCache =
                new ConcurrentDictionary<Type, Func<Operation, Exception, ExceptionHandlingStrategy>[]>();

        /// <summary>
        /// The cache implementation to use for this executor - this will
        /// be set by the CommandExecutor / QueryExecutor to the default
        /// implementation, if none is already set
        /// </summary>
        public ICache Cache { get; set; }

        /// <summary>
        /// The type of cache usage to enforce for this executor - defaulting
        /// to Full
        /// </summary>
        public CacheUsage CacheUsage { get; set; } = CacheUsage.Full;

        /// <summary>
        /// Invalidate the cache for this executor. Only override if you're
        /// doing work in addition to removing any cached value or if you have
        /// some custom cache key generation logic.
        /// </summary>
        protected void InvalidateCache()
        {
            var cacheKey = GenerateCacheKey();
            Cache.Remove(cacheKey);
        }

        /// <summary>
        /// Passes the logic of a query through the cache as necessary,
        /// taking into account the CacheUsage for the current executor.
        /// This will also apply to any sub-queries of a command decorated
        /// with [Cache]
        /// </summary>
        /// <param name="generator"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected T Through<T>(Func<T> generator)
        {
            switch (CacheUsage)
            {
                case CacheUsage.Bypass:
                {
                    return generator();
                }
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

        /// <summary>
        /// Caches the result of a query as configured by the [Cache] attribute
        /// on the current query.
        /// </summary>
        /// <param name="result"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
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

        /// <summary>
        /// Describes the expiration model for caching an item:
        /// - absolute / sliding expiration
        /// - whether or not caching is even enabled
        /// </summary>
        protected class CacheExpirationModel
        {
            /// <summary>
            /// When set, use this absolute expiration for caching
            /// </summary>
            public DateTime? AbsoluteExpiration { get; }

            /// <summary>
            /// When set, use this sliding expiration for caching
            /// </summary>
            public TimeSpan? SlidingExpiration { get; }

            /// <summary>
            /// Whether or not to cache at all - if no expiration
            /// has been set, 
            /// </summary>
            public bool Enabled =>
                AbsoluteExpiration.HasValue ||
                SlidingExpiration.HasValue;

            /// <summary>
            /// Create a new cache expiration which is effectively disabled
            /// </summary>
            public CacheExpirationModel()
            {
            }

            /// <summary>
            /// Create a cache expiration with the provided sliding
            /// expiration timespan
            /// </summary>
            /// <param name="slidingExpiration"></param>
            public CacheExpirationModel(TimeSpan slidingExpiration)
            {
                SlidingExpiration = slidingExpiration;
            }

            /// <summary>
            /// Create a cache expiration with the provided absolute
            /// expiration datetime
            /// </summary>
            /// <param name="absoluteExpiration"></param>
            public CacheExpirationModel(DateTime absoluteExpiration)
            {
                AbsoluteExpiration = absoluteExpiration;
            }
        }

        /// <summary>
        /// Generates the caching options for this command / query
        /// </summary>
        /// <returns></returns>
        // ReSharper disable once VirtualMemberNeverOverridden.Global
        protected virtual CacheExpirationModel GenerateCacheOptions()
        {
            if (MyCacheAttribute == null)
            {
                return new CacheExpirationModel();
            }

            return MyCacheAttribute.CacheExpiration == CQRS.CacheExpiration.Absolute
                ? new CacheExpirationModel(DateTime.Now.AddSeconds(MyCacheAttribute.TTL))
                : new CacheExpirationModel(TimeSpan.FromSeconds(MyCacheAttribute.TTL));
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
        // ReSharper disable once VirtualMemberNeverOverridden.Global
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
            return cur.PropertyType.ImplementsEnumerableGenericType()
                ? $"{cur.Name}::{GenerateEnumerableKeyPartFor(cur.PropertyType, cur.GetValue(this))}"
                : $"{cur.Name}:{cur.GetValue(this)}";
        }

        private string GenerateEnumerableKeyPartFor(
            Type propertyType,
            object collection)
        {
            if (collection is null)
            {
                return "(null)";
            }

            var itemType = propertyType.GetCollectionItemType();
            var method = CollectionToListGenericMethod.MakeGenericMethod(itemType);
            return method.Invoke(null, new object[] { collection, "," }) as string;
        }

        private static string CollectionToList<T>(
            IEnumerable<T> collection,
            string delimiter)
        {
            return string.Join(delimiter, collection);
        }

        private static readonly MethodInfo CollectionToListGenericMethod
            = typeof(BaseSqlExecutor)
                .GetMethod(nameof(CollectionToList), BindingFlags.Static | BindingFlags.NonPublic);

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
        protected IEnumerable<T> SelectMany<T>(string sql)
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
        protected IEnumerable<T> SelectMany<T>(
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
        /// Selects zero or more items from the database
        /// </summary>
        /// <param name="sql"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected List<T> SelectList<T>(string sql)
        {
            return SelectList<T>(sql, null);
        }

        /// <summary>
        /// Selects zero or more items from the database
        /// </summary>
        /// <param name="sql"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected T[] SelectArray<T>(string sql)
        {
            return SelectArray<T>(sql, null);
        }

        /// <summary>
        /// Selects zero or more items from the database
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected T[] SelectArray<T>(
            string sql,
            object parameters
        )
        {
            return SelectMany<T>(sql, parameters)
                .ToArray();
        }

        /// <summary>
        /// Selects zero or more items from the database
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected List<T> SelectList<T>(
            string sql,
            object parameters
        )
        {
            return SelectMany<T>(sql, parameters)
                .ToList();
        }

        /// <summary>
        /// Selects the first matching item from the database
        /// or throws an EntityDoesNotExist exception if not found
        /// </summary>
        /// <param name="sql"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected T SelectFirst<T>(
            string sql)
        {
            return SelectFirst<T>(sql, null);
        }

        /// <summary>
        /// Selects the first matching item from the database
        /// or returns default(T) if not found
        /// </summary>
        /// <param name="sql"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected T SelectFirstOrDefault<T>(
            string sql
        )
        {
            return SelectFirstOrDefault<T>(
                sql,
                null
            );
        }

        /// <summary>
        /// Selects the first matching item from the database or
        /// returns default(T) if not found
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected T SelectFirstOrDefault<T>(
            string sql,
            object parameters
        )
        {
            return SelectMany<T>(
                sql,
                parameters
            ).FirstOrDefault();
        }

        /// <summary>
        /// Selects the first matching item from the database
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected T SelectFirst<T>(
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
        protected IEnumerable<TReturn> SelectMulti<TFirst, TSecond, TReturn>(
            string sql,
            Func<TFirst, TSecond, TReturn> function)

        {
            return SelectMulti(
                sql,
                null,
                function);
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
        protected IEnumerable<TReturn> SelectMulti<TFirst, TSecond, TReturn>(
            string sql,
            object parameters,
            Func<TFirst, TSecond, TReturn> function)
        {
            return SelectMulti(
                sql,
                parameters,
                function,
                DEFAULT_SPLIT_ON_COLUMN
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
        /// <param name="parameters">The parameters.</param>
        /// <param name="splitOn">Sets the name of the column to split type mappings at - other members default this to "Id"</param>
        /// <returns></returns>
        protected IEnumerable<TReturn> SelectMulti<TFirst, TSecond, TReturn>(
            string sql,
            object parameters,
            Func<TFirst, TSecond, TReturn> function,
            string splitOn)
        {
            return Through(() =>
            {
                EnsureDapperKnowsAbout<TFirst>();
                EnsureDapperKnowsAbout<TSecond>();
                using var connection = CreateOpenConnection();

                List<TReturn> Exec(IDbConnection conn)
                {
                    return conn.Query(
                        sql,
                        function,
                        parameters,
                        splitOn: splitOn
                    ).ToList();
                }

                return SelectRowsOnConnection(connection, Exec);
            });
        }

        /// <summary>
        /// Performs a one-to-many select query across tables joined
        /// by an integer id
        /// </summary>
        /// <param name="sql">select statement to run</param>
        /// <param name="idFinder">function to find the id off of a primary item</param>
        /// <param name="collectionFinder">function to find the collection on the primary item</param>
        /// <typeparam name="TPrimary">type of the primary item</typeparam>
        /// <typeparam name="TSecondary">type of the secondary item</typeparam>
        /// <returns></returns>
        protected IEnumerable<TPrimary> SelectOneToMany<TPrimary, TSecondary>(
            string sql,
            Func<TPrimary, int> idFinder,
            Func<TPrimary, IList<TSecondary>> collectionFinder)
        {
            return SelectOneToMany(
                sql,
                null,
                idFinder,
                collectionFinder
            );
        }

        /// <summary>
        /// Performs a one-to-many select query across tables joined
        /// by an integer id
        /// </summary>
        /// <param name="sql">select statement to run</param>
        /// <param name="parameters">parameters for the sql statement</param>
        /// <param name="idFinder">function to find the id off of a primary item</param>
        /// <param name="collectionFinder">function to find the collection on the primary item</param>
        /// <typeparam name="TPrimary">type of the primary item</typeparam>
        /// <typeparam name="TSecondary">type of the secondary item</typeparam>
        /// <returns></returns>
        protected IEnumerable<TPrimary> SelectOneToMany<TPrimary, TSecondary>(
            string sql,
            object parameters,
            Func<TPrimary, int> idFinder,
            Func<TPrimary, IList<TSecondary>> collectionFinder)
        {
            return SelectOneToMany<TPrimary, TSecondary, int>(
                sql,
                parameters,
                idFinder,
                collectionFinder
            );
        }

        /// <summary>
        /// Performs a one-to-many select query across tables joined
        /// by an id of type TId
        /// </summary>
        /// <param name="sql">select statement to run</param>
        /// <param name="idFinder">function to find the id off of a primary item</param>
        /// <param name="collectionFinder">function to find the collection on the primary item</param>
        /// <typeparam name="TPrimary">type of the primary item</typeparam>
        /// <typeparam name="TSecondary">type of the secondary item</typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <returns></returns>
        protected IEnumerable<TPrimary> SelectOneToMany<TPrimary, TSecondary, TId>(
            string sql,
            Func<TPrimary, TId> idFinder,
            Func<TPrimary, IList<TSecondary>> collectionFinder
        )
        {
            return SelectOneToMany(
                sql,
                null,
                idFinder,
                collectionFinder
            );
        }

        /// <summary>
        /// Performs a one-to-many select query across tables joined
        /// by an id of type TId
        /// </summary>
        /// <param name="sql">select statement to run</param>
        /// <param name="parameters">parameters for the sql statement</param>
        /// <param name="idFinder">function to find the id off of a primary item</param>
        /// <param name="collectionFinder">function to find the collection on the primary item</param>
        /// <typeparam name="TPrimary">type of the primary item</typeparam>
        /// <typeparam name="TSecondary">type of the secondary item</typeparam>
        /// <typeparam name="TId">Type of the identifier on TPrimary</typeparam>
        /// <returns></returns>
        protected IEnumerable<TPrimary> SelectOneToMany<TPrimary, TSecondary, TId>(
            string sql,
            object parameters,
            Func<TPrimary, TId> idFinder,
            Func<TPrimary, IList<TSecondary>> collectionFinder
        )
        {
            return SelectOneToMany(
                sql,
                parameters,
                idFinder,
                collectionFinder,
                o => o
            );
        }

        /// <summary>
        /// Performs a one-to-many select query across tables joined
        /// by an integer id where the final result is a composite type
        /// </summary>
        /// <param name="sql">select statement to run</param>
        /// <param name="idFinder">function to find the id off of a primary item</param>
        /// <param name="collectionFinder">function to find the collection on the primary item</param>
        /// <param name="returnFactory">factory to create objects of the final return type based on the primary type</param>
        /// <typeparam name="TPrimary">type of the primary item</typeparam>
        /// <typeparam name="TSecondary">type of the secondary item</typeparam>
        /// <typeparam name="TReturn"></typeparam>
        /// <returns></returns>
        protected IEnumerable<TReturn> SelectOneToMany<TPrimary, TSecondary, TReturn>(
            string sql,
            Func<TPrimary, int> idFinder,
            Func<TReturn, IList<TSecondary>> collectionFinder,
            Func<TPrimary, TReturn> returnFactory
        )
        {
            return SelectOneToMany(
                sql,
                null,
                idFinder,
                collectionFinder,
                returnFactory
            );
        }

        /// <summary>
        /// Performs a one-to-many select query across tables joined
        /// by an integer id where the final result is a composite type
        /// </summary>
        /// <param name="sql">select statement to run</param>
        /// <param name="parameters">parameters for the sql statement</param>
        /// <param name="idFinder">function to find the id off of a primary item</param>
        /// <param name="collectionFinder">function to find the collection on the primary item</param>
        /// <param name="returnFactory">factory to create objects of the final return type based on the primary type</param>
        /// <typeparam name="TPrimary">type of the primary item</typeparam>
        /// <typeparam name="TSecondary">type of the secondary item</typeparam>
        /// <typeparam name="TReturn"></typeparam>
        /// <returns></returns>
        protected IEnumerable<TReturn> SelectOneToMany<TPrimary, TSecondary, TReturn>(
            string sql,
            object parameters,
            Func<TPrimary, int> idFinder,
            Func<TReturn, IList<TSecondary>> collectionFinder,
            Func<TPrimary, TReturn> returnFactory
        )
        {
            return SelectOneToMany<TPrimary, TSecondary, TReturn, int>(
                sql,
                parameters,
                idFinder,
                collectionFinder,
                returnFactory
            );
        }

        /// <summary>
        /// Performs a one-to-many select query across tables joined
        /// by an id of type TId where the final result is a composite type
        /// </summary>
        /// <param name="sql">select statement to run</param>
        /// <param name="idFinder">function to find the id off of a primary item</param>
        /// <param name="collectionFinder">function to find the collection on the primary item</param>
        /// <param name="returnFactory">factory to create objects of the final return type based on the primary type</param>
        /// <typeparam name="TPrimary">type of the primary item</typeparam>
        /// <typeparam name="TSecondary">type of the secondary item</typeparam>
        /// <typeparam name="TReturn"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <returns></returns>
        protected IEnumerable<TReturn> SelectOneToMany<TPrimary, TSecondary, TReturn, TId>(
            string sql,
            Func<TPrimary, TId> idFinder,
            Func<TReturn, IList<TSecondary>> collectionFinder,
            Func<TPrimary, TReturn> returnFactory
        )
        {
            return SelectOneToMany(
                sql,
                null,
                idFinder,
                collectionFinder,
                returnFactory
            );
        }

        /// <summary>
        /// Performs a one-to-many select query across tables joined
        /// by an id of type TId where the final result is a composite type
        /// </summary>
        /// <param name="sql">select statement to run</param>
        /// <param name="parameters">parameters for the sql statement</param>
        /// <param name="idFinder">function to find the id off of a primary item</param>
        /// <param name="collectionFinder">function to find the collection on the primary item</param>
        /// <param name="returnFactory">factory to create objects of the final return type based on the primary type</param>
        /// <typeparam name="TPrimary">type of the primary item</typeparam>
        /// <typeparam name="TSecondary">type of the secondary item</typeparam>
        /// <typeparam name="TReturn"></typeparam>
        /// <typeparam name="TId"></typeparam>
        /// <returns></returns>
        protected IEnumerable<TReturn> SelectOneToMany<TPrimary, TSecondary, TReturn, TId>(
            string sql,
            object parameters,
            Func<TPrimary, TId> idFinder,
            Func<TReturn, IList<TSecondary>> collectionFinder,
            Func<TPrimary, TReturn> returnFactory)
        {
            return SelectOneToMany(
                sql,
                parameters,
                idFinder,
                collectionFinder,
                returnFactory,
                DEFAULT_SPLIT_ON_COLUMN
            );
        }

        /// <summary>
        /// Performs a one-to-many select query across tables joined
        /// by an id of type TId where the final result is a composite type
        /// </summary>
        /// <param name="sql">select statement to run</param>
        /// <param name="parameters">parameters for the sql statement</param>
        /// <param name="idFinder">function to find the id off of a primary item</param>
        /// <param name="collectionFinder">function to find the collection on the primary item</param>
        /// <param name="returnFactory">factory to create objects of the final return type based on the primary type</param>
        /// <param name="splitOn"></param>
        /// <typeparam name="TPrimary">type of the primary item</typeparam>
        /// <typeparam name="TSecondary">type of the secondary item</typeparam>
        /// <typeparam name="TReturn">return type</typeparam>
        /// <typeparam name="TId">type of the Id column</typeparam>
        /// <returns></returns>
        protected IEnumerable<TReturn> SelectOneToMany<TPrimary, TSecondary, TReturn, TId>(
            string sql,
            object parameters,
            Func<TPrimary, TId> idFinder,
            Func<TReturn, IList<TSecondary>> collectionFinder,
            Func<TPrimary, TReturn> returnFactory,
            string splitOn)
        {
            var allResults = SelectMulti<TPrimary, TSecondary, ValueTuple<TPrimary, TSecondary>>(
                sql,
                parameters,
                (one, many) => (one, many),
                splitOn
            );
            var lookup = new Dictionary<TId, TReturn>();
            var result = allResults.Aggregate(
                new List<TReturn>(),
                (acc, cur) =>
                {
                    var id = idFinder(cur.Item1);
                    if (!lookup.TryGetValue(id, out var returnItem))
                    {
                        returnItem = returnFactory(cur.Item1);
                        lookup[id] = returnItem;
                        acc.Add(returnItem);
                    }

                    var collection = collectionFinder(returnItem);
                    collection.Add(cur.Item2);
                    return acc;
                });
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
        protected IEnumerable<TReturn> SelectMulti<TFirst, TSecond, TThird, TReturn>(
            string sql,
            Func<TFirst, TSecond, TThird, TReturn> function)
        {
            return SelectMulti(
                sql,
                null,
                function);
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
            object parameters,
            Func<TFirst, TSecond, TThird, TReturn> function)
        {
            return SelectMulti(
                sql,
                parameters,
                function,
                DEFAULT_SPLIT_ON_COLUMN
            );
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
        /// <param name="splitOn">Sets the name of the column to split type mappings at - other members default this to "Id"</param>
        /// <returns></returns>
        protected IEnumerable<TReturn> SelectMulti<TFirst, TSecond, TThird, TReturn>(
            string sql,
            object parameters,
            Func<TFirst, TSecond, TThird, TReturn> function,
            string splitOn)
        {
            return Through(() =>
            {
                EnsureDapperKnowsAbout<TFirst>();
                EnsureDapperKnowsAbout<TSecond>();
                EnsureDapperKnowsAbout<TThird>();
                using var connection = CreateOpenConnection();

                List<TReturn> Exec(IDbConnection conn)
                {
                    return conn.Query(sql, function, parameters, splitOn: splitOn).ToList();
                }

                return SelectRowsOnConnection(
                    connection,
                    Exec
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
                if (ShouldThrowFor(Operation.Select, ex))
                {
                    throw;
                }

                return default;
            }
        }

        private Func<Operation, Exception, ExceptionHandlingStrategy>[] FindHandlersFor(
            Exception ex
        )
        {
            var exType = ex.GetType();
            if (ExceptionHandlerCache.TryGetValue(exType, out var cached))
            {
                return cached;
            }

            Func<Operation, Exception, ExceptionHandlingStrategy>[] result = null;
            lock (ExceptionHandlers)
            {
                if (ExceptionHandlers.TryGetValue(ex.GetType(), out var handlers))
                {
                    result = handlers
                        .Select(h => h.Item2)
                        .ToArray();
                }
            }

            result ??= Array.Empty<Func<Operation, Exception, ExceptionHandlingStrategy>>();
            ExceptionHandlerCache.TryAdd(exType, result);

            // TODO: try to find derived handler? probably travel all the way up to Exception -- would be useful
            // for the caller to be able to install a generic Exception handler
            return result;
        }


        private bool ShouldThrowFor(Operation operation, Exception ex)
        {
            var handlers = FindHandlersFor(ex);
            if (!handlers.Any())
            {
                return true; // throw by default
            }

            return handlers.Aggregate(
                false,
                (acc, cur) =>
                    // throw if any handler says so
                    acc || cur.Invoke(operation, ex) == ExceptionHandlingStrategy.Throw
            );
        }


        /// <summary>
        /// Used to map results from a query which returns multiple
        /// result sets, ie multiple select statements
        /// </summary>
        /// <param name="sql">The SQL query.</param>
        /// <param name="callback">The callback action on how to handle the results.</param>
        protected void SelectMulti(
            string sql,
            Action<SqlMapper.GridReader> callback)
        {
            SelectMulti(
                sql,
                null,
                callback
            );
        }

        /// <summary>
        /// Used to map results from a query which returns multiple
        /// result sets, ie multiple select statements
        /// </summary>
        /// <param name="sql">The SQL query.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="callback">The callback action on how to handle the results.</param>
        protected void SelectMulti(
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
                if (ShouldThrowFor(Operation.Select, ex))
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
        protected IEnumerable<T> UpdateGetList<T>(
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
        protected IEnumerable<T> UpdateGetList<T>(
            string sql,
            object parameters)
        {
            return QueryCollection<T>(Operation.Update, sql, parameters);
        }

        /// <summary>
        /// Performs an update operation where the last sql query in the batch
        /// is a select, returning only the first item from that select.
        /// </summary>
        /// <param name="sql"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected T UpdateGetFirst<T>(string sql)
        {
            return UpdateGetFirst<T>(sql, null);
        }

        /// <summary>
        /// Performs an update operation where the last sql query in the batch
        /// is a select, returning only the first item from that select.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected T UpdateGetFirst<T>(string sql, object parameters)
        {
            return Through(
                () => QueryFirst<T>(Operation.Update, sql, parameters)
            );
        }

        /// <summary>
        /// Performs an update operation where the last sql query in the batch
        /// is a select, returning all items from that select.
        /// </summary>
        /// <param name="sql"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected IEnumerable<T> UpdateGetAll<T>(string sql)
        {
            return UpdateGetAll<T>(sql, null);
        }

        /// <summary>
        /// Performs an update operation where the last sql query in the batch
        /// is a select, returning all items from that select.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected IEnumerable<T> UpdateGetAll<T>(string sql, object parameters)
        {
            return Through(
                () => QueryCollection<T>(Operation.Update, sql, parameters)
            );
        }

        /// <summary>
        /// Performs an update operation where the last sql query in the batch
        /// is a select, returning the results from that query.
        /// </summary>
        /// <param name="sql"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected IEnumerable<T> InsertGetAll<T>(string sql)
        {
            return InsertGetAll<T>(sql, null);
        }

        /// <summary>
        /// Performs an insert operation where the last sql query in the batch
        /// is a select, returning the results from that query.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected IEnumerable<T> InsertGetAll<T>(string sql, object parameters)
        {
            return QueryCollection<T>(Operation.Insert, sql, parameters);
        }

        /// <summary>
        /// Performs an insert operation where the last sql query in the batch
        /// is a select, returning all items from that select.
        /// </summary>
        /// <param name="sql"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected T InsertGetFirst<T>(string sql)
        {
            return InsertGetFirst<T>(sql, null);
        }

        /// <summary>
        /// Performs an insert operation where the last sql query in the batch
        /// is a select, returning all items from that select.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected T InsertGetFirst<T>(string sql, object parameters)
        {
            return QueryFirst<T>(Operation.Insert, sql, parameters);
        }

        /// <summary>
        /// Performs a delete operation where the last sql query in the batch
        /// is a select, returning all items from that select.
        /// </summary>
        /// <param name="sql"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected IEnumerable<T> DeleteGetAll<T>(string sql)
        {
            return DeleteGetAll<T>(sql, null);
        }

        /// <summary>
        /// Performs a delete operation where the last sql query in the batch
        /// is a select, returning all items from that select.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected IEnumerable<T> DeleteGetAll<T>(string sql, object parameters)
        {
            return QueryCollection<T>(Operation.Delete, sql, parameters);
        }

        /// <summary>
        /// Performs a delete operation where the last sql query in the batch
        /// is a select, returning the first item from that select.
        /// </summary>
        /// <param name="sql"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected T DeleteGetFirst<T>(string sql)
        {
            return DeleteGetFirst<T>(sql, null);
        }

        /// <summary>
        /// Performs a delete operation where the last sql query in the batch
        /// is a select, returning the first item from that select.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        protected T DeleteGetFirst<T>(string sql, object parameters)
        {
            return QueryFirst<T>(Operation.Delete, sql, parameters);
        }

        /// <summary>
        /// Executes an update with no return
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        protected int ExecuteUpdate(string sql)
        {
            return ExecuteUpdate(sql, null);
        }

        /// <summary>
        /// Executes an update with no return
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        protected int ExecuteUpdate(string sql, object parameters)
        {
            return Execute(Operation.Update, sql, parameters);
        }

        /// <summary>
        /// Executes an insert with no return
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        protected int ExecuteInsert(string sql)
        {
            return ExecuteInsert(sql, null);
        }

        /// <summary>
        /// Executes an insert with no return
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        protected int ExecuteInsert(string sql, object parameters)
        {
            return Execute(Operation.Insert, sql, parameters);
        }

        /// <summary>
        /// Executes a delete with no return
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        protected int ExecuteDelete(string sql)
        {
            return ExecuteDelete(sql, null);
        }

        /// <summary>
        /// Executes a delete with no return
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        protected int ExecuteDelete(string sql, object parameters)
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
                if (!ShouldThrowFor(operation, ex))
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
                            throw new EntityNotFoundException(
                                typeof(T).Name,
                                parameters,
                                ex
                            );
                        }

                        if (ShouldThrowFor(operation, ex))
                        {
                            throw;
                        }

                        return default;
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
                if (ShouldThrowFor(operation, ex))
                {
                    throw;
                }

                return default;
            }
        }

        /// <summary>
        /// The most base mutation execution, returning the number
        /// of rows affected.
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <param name="commandTimeout"></param>
        /// <returns></returns>
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

        /// <summary>
        /// The most base mutation execution, returning the number
        /// of rows affected, utilizing the provided exception handler.
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <param name="commandTimeout"></param>
        /// <param name="customExceptionHandler"></param>
        /// <typeparam name="TException"></typeparam>
        /// <returns></returns>
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
                if (!(customExceptionHandler is null) &&
                    ex is TException handledException)
                {
                    var customHandlerResult = customExceptionHandler.Handle(
                        operation,
                        handledException
                    );
                    if (customHandlerResult == ExceptionHandlingStrategy.Suppress)
                    {
                        return default;
                    }
                }

                if (ShouldThrowFor(operation, ex))
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

        /// <summary>
        /// Clears all statically-defined exception handlers.
        /// </summary>
        public static void RemoveAllExceptionHandlers()
        {
            lock (ExceptionHandlers)
            {
                ExceptionHandlers.Clear();
            }
        }

        private IDbConnection CreateOpenConnection()
        {
            var result = ConnectionFactory?.CreateFor(this)
                ?? throw new InvalidOperationException(
                    "Please configure a ConnectionFactory to provide new instances of IDbConnection per call to Create()");
            ;
            if (result.State != ConnectionState.Open)
            {
                result.Open();
            }

            return result;
        }
    }
}