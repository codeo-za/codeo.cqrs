using System;
using System.Collections.Generic;

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
        public HashSet<string> CacheKeyProperties { get; }
        public CacheExpiration CacheExpiration { get; }
        public int TTL { get; }

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