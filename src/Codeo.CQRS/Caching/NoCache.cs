using System;

namespace Codeo.CQRS.Caching
{
    public class NoCache : ICache
    {
        public bool ContainsKey(string key)
        {
            return false;
        }

        public void Set(string key, object value)
        {
        }

        public void Set(string key, object value, DateTime absoluteExpiration)
        {
        }

        public void Set(string key, object value, TimeSpan slidingExpiration)
        {
        }

        public object Get(string key)
        {
            return null;
        }

        public T Get<T>(string key)
        {
            return default(T);
        }

        public T GetOrDefault<T>(string key, T defaultValue)
        {
            return defaultValue;
        }

        public T GetOrSet<T>(string key, Func<T> generator)
        {
            return generator();
        }

        public T GetOrSet<T>(string key, Func<T> generator, TimeSpan slidingExpiration)
        {
            return generator();
        }

        public T GetOrSet<T>(string key, Func<T> generator, DateTime absoluteExpiration)
        {
            return generator();
        }

        public void Remove(string key)
        {
        }

        public void RemoveAll()
        {
        }
    }
}