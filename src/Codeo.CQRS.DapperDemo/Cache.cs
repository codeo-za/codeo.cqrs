using Codeo.CQRS.Caching;

namespace Codeo.CQRS.Demo;

public class Cache : ICache
{
    public void Dispose()
    {
        
    }

    public bool ContainsKey(string key)
    {
        throw new NotImplementedException();
    }

    public void Set(string key, object value)
    {
        throw new NotImplementedException();
    }

    public void Set(string key, object value, DateTime absoluteExpiration)
    {
        throw new NotImplementedException();
    }

    public void Set(string key, object value, TimeSpan slidingExpiration)
    {
        throw new NotImplementedException();
    }

    public object Get(string key)
    {
        throw new NotImplementedException();
    }

    public T Get<T>(string key)
    {
        throw new NotImplementedException();
    }

    public T GetOrDefault<T>(string key, T defaultValue)
    {
        throw new NotImplementedException();
    }

    public T GetOrSet<T>(string key, Func<T> generator)
    {
        throw new NotImplementedException();
    }

    public T GetOrSet<T>(string key, Func<T> generator, TimeSpan slidingExpiration)
    {
        throw new NotImplementedException();
    }

    public T GetOrSet<T>(string key, Func<T> generator, DateTime absoluteExpiration)
    {
        throw new NotImplementedException();
    }

    public void Remove(string key)
    {
        throw new NotImplementedException();
    }

    public void RemoveAll()
    {
        throw new NotImplementedException();
    }
}