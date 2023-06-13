namespace Codeo.CQRS.ContextProviders;

public class DapperContextProvider : ISqlExecutor
{
    public IEnumerable<T> SelectMany<T>(string sql)
    {
        return Enumerable.Empty<T>();
    }
}