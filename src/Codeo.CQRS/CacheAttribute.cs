using System;
using System.Collections.Generic;
using System.Runtime.Caching;

namespace Codeo.CQRS
{
    public enum CacheExpiration
    {
        Absolute,
        Sliding
    }

    /// <summary>
    /// Apply caching to your queries
    /// </summary>
    public class CacheAttribute : Attribute
    {
        // this is how many seconds are in a year
        // https://www.rapidtables.com/calc/time/seconds-in-year.html
        public const int ONE_YEAR = 31622400;
        public HashSet<string> CacheKeyProperties { get; }
        public CacheExpiration CacheExpiration { get; }
        public int TTL { get; }

        public CacheAttribute(params string[] cacheKeyProperties)
            : this(ONE_YEAR, cacheKeyProperties)
        {
        }

        public CacheAttribute(
            int ttl,
            CacheExpiration expiration,
            params string[] cacheKeyProperties)
        {
            TTL = ttl;
            CacheKeyProperties = new HashSet<string>(cacheKeyProperties);
            CacheExpiration = expiration;
        }

        public CacheAttribute(
            int ttl,
            params string[] cacheKeyProperties
        ) : this(ttl, CacheExpiration.Absolute, cacheKeyProperties)
        {
        }
    }
}