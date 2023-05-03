using System;
using System.Data;
using System.Data.Common;
using Dapper;
using NUnit.Framework;
using PeanutButter.TempDb.MySql.Data;

namespace Codeo.CQRS.Tests
{
    [SetUpFixture]
    public class GlobalSetup
    {
        private static TempDBMySql _db;

        public static void OneTimeSetup()
        {
            if (_db != null)
            {
                // already set up
                return;
            }

            _db = new TempDBMySql();
            PerformDefaultConfiguration();
            CreateBasicSchemaWith(_db.OpenConnection());
        }

        public static void PerformDefaultConfiguration()
        {
            Fluently.Configure()
                .WithConnectionFactory(new TempDbConnectionFactory(_db))
                .WithEntitiesFrom(typeof(TestQueryExecution).Assembly);
        }

        private static void CreateBasicSchemaWith(DbConnection connection)
        {
            connection.Query(@"
create table people(
  id integer not null primary key auto_increment, 
  name text,
  enabled bit,
  date_of_birth datetime null,
  created datetime);
create table departments(
  id integer not null primary key auto_increment,
  name tinytext
);
create table departments_people(
    id integer not null primary key auto_increment,
    department_id int not null,
    person_id int not null
);
create table departments_tags(
    id integer not null primary key auto_increment,
    department_id int not null,
    tag tinytext not null
);
");
            connection.Query(
                "insert into people(name, date_of_birth, enabled, created) values ('Carl Sagan', '1934/11/09', 1,  CURRENT_TIMESTAMP);");
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _db?.Dispose();
            _db = null;
        }

        public static IDbConnection ConnectToTempDb()
        {
            return _db?.OpenConnection() ??
                throw new InvalidOperationException(
                    $"TempDb is not set up"
                );
            ;
        }
    }

    public abstract class TestFixtureRequiringData
    {
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            GlobalSetup.OneTimeSetup();
        }
    }
}