using System;

namespace Codeo.CQRS.Caching
{
    /// <summary>
    /// Represents a class that can be used to store cached information.
    /// </summary>
    public interface ICache : IDisposable
    {
        /// <summary>
        /// Determines whether the cache contains an item with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>
        /// 	<c>true</c> if the cache contains key; otherwise, <c>false</c>.
        /// </returns>
        bool ContainsKey(string key);

        /// <summary>
        /// Caches <c>value</c> with the specified <c>key</c>.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <remarks>This object will live in the cache for the lifetime of the application.</remarks>
        void Set(string key, object value);

        /// <summary>
        /// Caches <c>value</c> with the specified <c>key</c>, until a specified time.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="absoluteExpiration">The time when the item is automatically removed from the cache.</param>
        void Set(string key, object value, DateTime absoluteExpiration);

        /// <summary>
        /// Caches <c>value</c> with the specified <c>key</c>, for a SLIDING specified amount of time.
        /// Whenever the item is accessed in the cache, the expiration time will be extended. WARNING:
        /// this means that hot data is likely to remain in the cache and never be updated.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="slidingExpiration">The amount of time the item should remain in the cache before getting removed.  If the item is accessed, this time will reset to 0.</param>
        void Set(string key, object value, TimeSpan slidingExpiration);

        /// <summary>
        /// Gets an object from the cache.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        object Get(string key);

        /// <summary>
        /// Gets an object of a specified type from the cache.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        T Get<T>(string key);

        /// <summary>
        /// Gets an object of a specified type from the cache, with a default value if it does not exist.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns></returns>
        T GetOrDefault<T>(string key, T defaultValue);

        /// <summary>
        /// Gets a value from the cache, if it doesn't exist, the generator func is used to generate the value and this value is set in cache
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="generator"></param>
        /// <returns></returns>
        T GetOrSet<T>(string key, Func<T> generator);

        /// <summary>
        /// Gets a value from the cache, if it doesn't exist, the generator func is used to generate the value and this value is set in cache
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="generator"></param>
        /// <param name="slidingExpiration"></param>
        /// <returns></returns>
        T GetOrSet<T>(string key, Func<T> generator, TimeSpan slidingExpiration);

        /// <summary>
        /// Gets a value from the cache, if it doesn't exist, the generator func is used to generate the value and this value is set in cache
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="generator"></param>
        /// <param name="absoluteExpiration"></param>
        /// <returns></returns>
        T GetOrSet<T>(string key, Func<T> generator, DateTime absoluteExpiration);

        /// <summary>
        /// Explicitly removes an object with the specified key from the cache.
        /// </summary>
        /// <param name="key">The key.</param>
        void Remove(string key);

        /// <summary>
        /// Removes all items from the cache
        /// </summary>
        void Clear();
    }
}