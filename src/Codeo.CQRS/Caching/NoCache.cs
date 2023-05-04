using System;

namespace Codeo.CQRS.Caching
{
    /// <summary>
    /// The null-implementation for caching (ie, cache nothing)
    /// </summary>
    public class NoCache : ICache
    {
        /// <inheritdoc />
        public int Count => 0;

        /// <summary>
        /// Always returns false
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool ContainsKey(string key)
        {
            return false;
        }

        /// <summary>
        /// No-op
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Set(string key, object value)
        {
        }

        /// <summary>
        /// No-op
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="absoluteExpiration"></param>
        public void Set(string key, object value, DateTime absoluteExpiration)
        {
        }

        /// <summary>
        /// No-op
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="slidingExpiration"></param>
        public void Set(string key, object value, TimeSpan slidingExpiration)
        {
        }

        /// <summary>
        /// No-op
        /// </summary>
        /// <param name="key"></param>
        public object Get(string key)
        {
            return null;
        }

        /// <summary>
        /// No-op
        /// </summary>
        /// <param name="key"></param>
        public T Get<T>(string key)
        {
            return default(T);
        }

        /// <summary>
        /// No-op
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        public T GetOrDefault<T>(
            string key,
            T defaultValue
        )
        {
            return defaultValue;
        }

        /// <summary>
        /// Runs the generator and returns the value from it.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="generator"></param>
        public T GetOrSet<T>(
            string key,
            Func<T> generator
        )
        {
            return generator();
        }

        /// <summary>
        /// Runs the generator and returns the value from it.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="generator"></param>
        /// <param name="slidingExpiration"></param>
        public T GetOrSet<T>(
            string key,
            Func<T> generator,
            TimeSpan slidingExpiration
        )
        {
            return generator();
        }

        /// <summary>
        /// Runs the generator and returns the value from it.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="generator"></param>
        /// <param name="absoluteExpiration"></param>
        public T GetOrSet<T>(
            string key,
            Func<T> generator,
            DateTime absoluteExpiration
        )
        {
            return generator();
        }

        /// <summary>
        /// No-op
        /// </summary>
        /// <param name="key"></param>
        public void Remove(string key)
        {
        }

        /// <summary>
        /// No-op
        /// </summary>
        public void Clear()
        {
        }

        /// <summary>
        /// No-op
        /// </summary>
        public void Dispose()
        {
        }
    }
}