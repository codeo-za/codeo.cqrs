using System;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Caching;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Codeo.CQRS.Tests")]

namespace Codeo.CQRS.Caching
{
    /// <summary>
    /// Provides a simple in-memory cache, wrapping System.Runtime.Caching.ObjectCache
    /// </summary>
    public class MemoryCache : ICache
    {
        /// <summary>
        /// The default cache item policy to ob
        /// </summary>
        public static readonly CacheItemPolicy DefaultCachePolicy = new CacheItemPolicy();

        // for testing only
        internal ObjectCache Cache => _actual;
        private readonly ObjectCache _actual;

        /// <summary>
        /// Constructs a MemoryCache backed by
        /// System.Runtime.Caching.MemoryCache.Default
        /// </summary>
        public MemoryCache() :
            this(System.Runtime.Caching.MemoryCache.Default)
        {
        }

        /// <summary>
        /// Construct a memory cache with provided settings
        /// </summary>
        /// <param name="name"></param>
        /// <param name="cacheSizeInMb"></param>
        /// <param name="physicalMemoryLimitPercentage"></param>
        /// <param name="pollingInterval"></param>
        public MemoryCache(
            string name,
            long cacheSizeInMb,
            int physicalMemoryLimitPercentage,
            TimeSpan pollingInterval
        ) : this(
            name,
            CreateSettingsFrom(
                cacheSizeInMb,
                physicalMemoryLimitPercentage,
                pollingInterval
            )
        )
        {
        }

        /// <summary>
        /// Construct a memory cache backed by a custom implementation of ObjectCache
        /// </summary>
        /// <param name="cache"></param>
        public MemoryCache(ObjectCache cache)
        {
            _actual = cache;
        }

        /// <summary>
        /// Construct a named memory cache, perhaps from app settings
        /// Useful settings are:
        /// - CacheMemoryLimitMegabytes (integer)
        /// - PhysicalMemoryLimitPercentage (integer, 0 - 100)
        /// - PollingInterval (should be in the form HH:MM:SS)
        /// </summary>
        /// <param name="name"></param>
        /// <param name="settings"></param>
        private MemoryCache(
            string name,
            NameValueCollection settings
        )
        {
            _actual = new System.Runtime.Caching.MemoryCache(name, settings);
        }

        private static NameValueCollection CreateSettingsFrom(
            long cacheSize,
            int physicalMemoryLimitPercentage,
            TimeSpan pollingInterval)
        {
            return new NameValueCollection(3)
            {
                { "CacheMemoryLimitMegabytes", cacheSize.ToString() },
                { "PhysicalMemoryLimitPercentage", physicalMemoryLimitPercentage.ToString() },
                { "PollingInterval", pollingInterval.ToString() }
            };
        }

        /// <summary>
        /// Tests if the provided key can be found in the cache.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(string key)
        {
            return _actual.Contains(key);
        }

        /// <summary>
        /// Sets (or overwrites) the item in the cache identified by the provided key,
        /// using the default caching policy.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Set(
            string key,
            object value
        )
        {
            SetInternal(
                key,
                value,
                DefaultCachePolicy
            );
        }

        /// <summary>
        /// Set (or overwrite) the value in the cache identified by the provided
        /// key for the given absolute expiration datetime.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="absoluteExpiration"></param>
        public void Set(
            string key,
            object value,
            DateTime absoluteExpiration
        )
        {
            SetInternal(
                key,
                value,
                AbsoluteExpirationFor(absoluteExpiration)
            );
        }

        /// <summary>
        /// Set (or overwrite) the value in the cache identified by the provided
        /// key for the given sliding expiration datetime.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="slidingExpiration"></param>
        public void Set(
            string key,
            object value,
            TimeSpan slidingExpiration
        )
        {
            SetInternal(
                key,
                value,
                SlidingExpirationFor(slidingExpiration)
            );
        }

        /// <summary>
        /// Retrieves the keyed value from the cache.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public object Get(string key)
        {
            return _actual.Get(key);
        }

        /// <summary>
        /// Retrieves the typed keyed value from the cache
        /// </summary>
        /// <param name="key"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Get<T>(string key)
        {
            var result = _actual.Get(key);
            return result is null
                ? default
                : TryCast<T>(result);
        }

        private static T TryCast<T>(object value)
        {
            try
            {
                return (T) value;
            }
            catch
            {
                return TryConvert<T>(value);
            }
        }

        private static T TryConvert<T>(object value)
        {
            try
            {
                return (T) Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// Attempt to retrieve the keyed value from the cache - when
        /// not found, return the default value for T
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetOrDefault<T>(
            string key,
            T defaultValue
        )
        {
            var result = _actual.Get(key);
            return result is null
                ? defaultValue
                : TryCast<T>(result);
        }

        /// <summary>
        /// Attempt to retrieve the value from the cache, and, if not
        /// found, execute the generator, insert into the cache, and
        /// return that value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="generator"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetOrSet<T>(
            string key,
            Func<T> generator
        )
        {
            return FetchOrGenerate(
                key,
                generator,
                () => DefaultCachePolicy
            );
        }

        private T SetInternal<T>(
            string key,
            T value,
            CacheItemPolicy policy
        )
        {
            _actual.Set(key, value, policy);
            return value;
        }

        /// <summary>
        /// Attempt to retrieve the value from the cache, and, if not
        /// found, execute the generator, insert into the cache, with
        /// the provided sliding expiration timespan and return that
        /// value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="generator"></param>
        /// <param name="slidingExpiration"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetOrSet<T>(
            string key,
            Func<T> generator,
            TimeSpan slidingExpiration
        )
        {
            return FetchOrGenerate(
                key,
                generator,
                () => SlidingExpirationFor(slidingExpiration)
            );
        }

        /// <summary>
        /// Attempt to retrieve the value from the cache, and, if not
        /// found, execute the generator, insert into the cache, with
        /// the provided absolute expiration timespan and return that
        /// value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="generator"></param>
        /// <param name="absoluteExpiration"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetOrSet<T>(
            string key,
            Func<T> generator,
            DateTime absoluteExpiration
        )
        {
            return FetchOrGenerate(
                key,
                generator,
                () => AbsoluteExpirationFor(absoluteExpiration)
            );
        }

        private T FetchOrGenerate<T>(
            string key,
            Func<T> generator,
            Func<CacheItemPolicy> cacheItemPolicy)
        {
            if (TryGet<T>(key, out var result1))
            {
                return result1;
            }

            lock (_actual)
            {
                if (TryGet<T>(key, out var result2))
                {
                    return result2;
                }

                var toCache = generator();
                SetInternal(key, toCache, cacheItemPolicy());
                return toCache;
            }
        }

        private bool TryGet<T>(string key, out T cached)
        {
            var result = _actual.Get(key);
            if (result is null)
            {
                cached = default;
                return false;
            }

            cached = TryCast<T>(result);
            return true;
        }

        /// <summary>
        /// Removes an item from the cache, if it is in there.
        /// </summary>
        /// <param name="key"></param>
        public void Remove(
            string key)
        {
            _actual.Remove(key);
        }

        /// <summary>
        /// Clears the entire cache
        /// </summary>
        public void Clear()
        {
            lock (_actual)
            {
                var keys = _actual.Select(kvp => kvp.Key).ToArray();
                foreach (var key in keys)
                {
                    _actual.Remove(key);
                }
            }
        }

        private static CacheItemPolicy SlidingExpirationFor(TimeSpan expiration)
        {
            return new CacheItemPolicy()
            {
                SlidingExpiration = expiration
            };
        }

        private static CacheItemPolicy AbsoluteExpirationFor(
            DateTime expiration)
        {
            return new CacheItemPolicy()
            {
                AbsoluteExpiration = expiration
            };
        }

        /// <summary>
        /// Disposes the underlying object cache.
        /// </summary>
        public void Dispose()
        {
            var disposable = _actual as IDisposable;
            disposable?.Dispose();
        }
    }
}