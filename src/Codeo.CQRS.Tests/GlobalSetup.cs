using System.Data.Common;
using Dapper;
using NUnit.Framework;
using PeanutButter.TempDb.MySql;

namespace Codeo.CQRS.Tests
{
    [SetUpFixture]
    public class GlobalSetup
    {
        private TempDBMySql _db;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _db = new TempDBMySql();
            Fluently.Configure()
                    .WithConnectionProvider(() => _db.CreateConnection())
                    .WithEntitiesFrom(typeof(TestQuery).Assembly);
            CreateBasicSchemaWith(_db.CreateConnection());
        }

        private void CreateBasicSchemaWith(DbConnection connection)
        {
            connection.Query(@"
create table people(
  id integer not null primary key auto_increment, 
  name text,
  enabled bit,
  created datetime);
");
            connection.Query("insert into people(name, enabled, created) values ('Carl Sagan', 1, CURRENT_TIMESTAMP);");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _db?.Dispose();
            _db = null;
        }
    }
}