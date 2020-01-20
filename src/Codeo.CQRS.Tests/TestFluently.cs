using System.Collections;
using System.Reflection;
using Codeo.CQRS.Caching;
using Codeo.CQRS.Exceptions;
using Dapper;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using PeanutButter.Utils;
using NExpect;
using static NExpect.Expectations;

namespace Codeo.CQRS.Tests
{
    [TestFixture]
    public class TestFluently
    {
        [TearDown]
        public void TearDown()
        {
            // ensure that any subsequent tests get the default test configuration
            GlobalSetup.PerformDefaultConfiguration();
        }

        [SetUp]
        public void Setup()
        {
            Fluently.Configure().Reset();
        }

        [Test]
        public void ClearShouldClearAllConfig()
        {
            // Arrange
            using (new AutoResetter(() =>
            {
                GlobalSetup.PerformDefaultConfiguration();
                Fluently.Configure().Reset();
            }, GlobalSetup.PerformDefaultConfiguration))
            {
                // Act
                Fluently.Configure()
                    .Reset();
                // Assert
                // should remove exception handlers
                Expect(BaseSqlExecutor.ExceptionHandlers)
                    .To.Be.Empty();
                
                // should remove connection factory
                Expect(BaseSqlExecutor.ConnectionFactory)
                    .To.Be.Null();
                
                // should turn off debug for EntityDoesNotExist
                Expect(EntityDoesNotExistException.DebugEnabled)
                    .To.Be.False();
                
            }
        }

        public class MySqlExceptionHandler : IExceptionHandler<MySqlException>
        {
            public bool Handle(Operation operation, MySqlException exception)
            {
                // suppress the error
                return true;
            }
        }

        [Test]
        public void ShouldInvokeExceptionHandlersWithExactExceptionTypeMatch()
        {
            // Arrange
            GlobalSetup.PerformDefaultConfiguration();
            var faultyQuery = new GenericSelectQuery<int>("select count(*) from table_which_does_not_exist;");
            var handler = new MySqlExceptionHandler();
            // Act
            Fluently.Configure()
                .WithExceptionHandler(handler);
            Expect(() => QueryExecutor.Execute(faultyQuery))
                .Not.To.Throw();
            // test handler caching
            Expect(() => QueryExecutor.Execute(faultyQuery))
                .Not.To.Throw();
            // Assert
        }

        [Test]
        public void ShouldBeAbleToRemoveSpecificHandler()
        {
            // Arrange
            GlobalSetup.PerformDefaultConfiguration();
            var faultyQuery = new GenericSelectQuery<int>("select count(*) from table_which_does_not_exist;");
            var handler = new MySqlExceptionHandler();
            // Act
            Fluently.Configure()
                .WithExceptionHandler(handler);
            Expect(() => QueryExecutor.Execute(faultyQuery))
                .Not.To.Throw();
            Fluently.Configure()
                .WithoutExceptionHandler(handler);
            Expect(() => QueryExecutor.Execute(faultyQuery))
                .To.Throw<MySqlException>();
            Fluently.Configure()
                .WithExceptionHandler(handler);
            // test handler caching
            Expect(() => QueryExecutor.Execute(faultyQuery))
                .Not.To.Throw();
            // Assert
        }


        private static readonly ICache NoCache = new NoCache();
        private static readonly IQueryExecutor QueryExecutor = new QueryExecutor(NoCache);
    }

    public class GenericSelectQuery<T> : SelectQuery<T>
    {
        public GenericSelectQuery(string sql) : base(sql)
        {
        }
    }
}