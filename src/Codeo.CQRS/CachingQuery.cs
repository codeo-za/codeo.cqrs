namespace Codeo.CQRS
{
    /// <summary>
    /// Simplified caching query: always writes to
    /// the cache when the body is executed, but only
    /// reads from the cache when UseCache is set to true, ie:
    /// UseCache is false: ignores the cache for read: always executes, writes the value to the cache
    /// UseCache is true: attempts to read from the cache but on a miss, will execute &amp; write to the cache
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class CachingQuery<T> : Query<T>
    {
        /// <summary>
        /// Quick toggle: when set to true, full cache usage is employed. When set
        /// to false, the cache is bypassed for reading, but the read value will
        /// be stored in the cache for subsequent queries.
        /// </summary>
        public bool UseCache
        {
            get => CacheUsage == CacheUsage.Full;
            set => CacheUsage = value
                ? CacheUsage.Full
                : CacheUsage.WriteOnly;
        }

        /// <summary>
        /// Configure whether or not the query should read from
        /// the cache. Newly executed results will always be written
        /// to the cache based on decoration of derived queries with
        /// the [Cache] attribute.
        /// </summary>
        /// <param name="useCache"></param>
        protected CachingQuery(bool useCache)
        {
            UseCache = useCache;
        }
    }
}