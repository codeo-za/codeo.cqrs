using System.Data;

namespace Codeo.CQRS.Demo;

public class ConnectionFactory : IDbConnectionFactory
{
    private readonly IDbConnection _dbConnection;

    public ConnectionFactory(IDbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }
    
    public IDbConnection Create()
    {
        return _dbConnection;
    }
}