using System.Data;

namespace Codeo.CQRS
{
    public interface IDbConnectionFactory
    {
        IDbConnection Create();
    }
}