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
        public const int ONE_YEAR_IN_SECONDS = 31622400;
        public HashSet<string> CacheKeyProperties { get; }
        public CacheExpiration CacheExpiration { get; }
        public int TTL { get; }

        public CacheAttribute(params string[] cacheKeyProperties)
            : this(ONE_YEAR_IN_SECONDS, cacheKeyProperties)
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