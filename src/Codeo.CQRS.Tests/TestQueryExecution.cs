using System;
using System.Data.Common;
using Dapper;
using NUnit.Framework;
using PeanutButter.TempDb.MySql;
using NExpect;
using static NExpect.Expectations;

namespace Codeo.CQRS.MySql.Tests
{
    [TestFixture]
    public class TestQuery
    {
        public class FindCarlSaganQuery : Query<Person>
        {
            [Test]
            public void ShouldBeAbleToReadSingleResult()
            {
                // Arrange
                var queryExecutor = new QueryExecutor();
                // Act
                var result = queryExecutor.Execute(new FindCarlSaganQuery());
                // Assert
                Expect(result).Not.To.Be.Null();
                Expect(result.Name).To.Equal("Carl Sagan");
            }

            public override void Execute()
            {
                Result = SelectFirst<Person>("select * from people where name = 'Carl Sagan';");
            }
        }

        public class Person : IEntity
        {
            public string Name { get; set; }
            public bool Enabled { get; set; }
            public DateTime Created { get; set; }
        }
    }

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
            connection.Query("insert into people(name, enabled, created) values ('Carl Sagan', 1, NOW());");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _db?.Dispose();
            _db = null;
        }
    }

}