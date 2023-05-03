using System.Data;
using PeanutButter.TempDb;

namespace Codeo.CQRS.Tests
{
    public class TempDbConnectionFactory: IDbConnectionFactory
    {
        private readonly ITempDB _tempDb;

        public TempDbConnectionFactory(
            ITempDB tempDb)
        {
            _tempDb = tempDb;
        }

        public IDbConnection CreateFor(BaseSqlExecutor _)
        {
            return _tempDb?.OpenConnection();
        }
    }
}