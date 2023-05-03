using System.Data;

namespace Codeo.CQRS
{
    /// <summary>
    /// Creates database connections for command / query execution
    /// </summary>
    public interface IDbConnectionFactory
    {
        /// <summary>
        /// Creates a connection for the provided command or query,
        /// allowing different connections based on the command or query
        /// </summary>
        /// <param name="commandOrQuery"></param>
        /// <returns></returns>
        IDbConnection CreateFor(BaseSqlExecutor commandOrQuery);
    }
}