using System;
using System.Collections.Generic;
using static Codeo.CQRS.CacheTimes;

namespace Codeo.CQRS
{
    /// <summary>
    /// The type of cache expiration to apply
    /// </summary>
    public enum CacheExpiration
    {
        /// <summary>
        /// At the provided datetime, the item will be evicted from the cache
        /// </summary>
        Absolute,

        /// <summary>
        /// For as long as the item is read from the cache within the provided
        /// timespan, the time-to-live for the item will be pushed out by the
        /// provided timespan. Use this with caution: if the data is hot,
        /// it will likely never be evicted automatically.
        /// </summary>
        Sliding
    }

    /// <summary>
    /// Apply caching to your queries
    /// </summary>
    public class CacheAttribute : Attribute
    {
        /// <summary>
        /// The names of properties to use for caching
        /// </summary>
        public HashSet<string> CacheKeyProperties { get; }

        /// <summary>
        /// The type of expiration to apply (absolute / sliding)
        /// </summary>
        public CacheExpiration CacheExpiration { get; }

        /// <summary>
        /// The time-to-live, in seconds, for cached items
        /// </summary>
        public int TTL { get; }

        /// <summary>
        /// Cache by the provided properties for a year. When no properties
        /// are supplied, all public properties for this query are used
        /// to generate the cache key (except UseCache).
        /// ie [Cache] - cache by all public properties for one year
        /// </summary>
        /// <param name="cacheKeyProperties"></param>
        public CacheAttribute(params string[] cacheKeyProperties)
            : this(ONE_YEAR_IN_SECONDS, cacheKeyProperties)
        {
        }

        /// <summary>
        /// Cache by the provided time-to-live in seconds and the
        /// optional list of properties. When no properties are provided,
        /// all public properties for this query are used to generate the
        /// cache key (except CacheKey).
        /// </summary>
        /// <param name="ttl"></param>
        /// <param name="cacheKeyProperties"></param>
        public CacheAttribute(
            int ttl,
            params string[] cacheKeyProperties
        ) : this(ttl, CacheExpiration.Absolute, cacheKeyProperties)
        {
        }

        /// <summary>
        /// Cache for the provided time-to-live in seconds, with the provided
        /// expiration (absolute / sliding), and the (optional) list of properties.
        /// When no properties are provided, all public properties for the query
        /// are used to generate the cache key (except UseCache).
        /// </summary>
        /// <param name="ttl"></param>
        /// <param name="expiration"></param>
        /// <param name="cacheKeyProperties"></param>
        public CacheAttribute(
            int ttl,
            CacheExpiration expiration,
            params string[] cacheKeyProperties)
        {
            TTL = ttl;
            CacheKeyProperties = new HashSet<string>(cacheKeyProperties);
            CacheExpiration = expiration;
        }
    }
}