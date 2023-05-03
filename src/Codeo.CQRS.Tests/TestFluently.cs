using System.Collections;
using System.Data;
using System.Reflection;
using Codeo.CQRS.Caching;
using Codeo.CQRS.Exceptions;
using Dapper;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using PeanutButter.Utils;
using NExpect;
using static NExpect.Expectations;
using static PeanutButter.RandomGenerators.RandomValueGen;

namespace Codeo.CQRS.Tests
{
    [TestFixture]
    public class TestFluently : TestFixtureRequiringData
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

        public class SuppressingMySqlExceptionHandler : IExceptionHandler<MySqlException>
        {
            public ExceptionHandlingStrategy Handle(Operation operation, MySqlException exception)
            {
                // suppress the error
                return ExceptionHandlingStrategy.Suppress;
            }
        }

        [Test]
        public void ShouldInvokeExceptionHandlersWithExactExceptionTypeMatch()
        {
            // Arrange
            GlobalSetup.PerformDefaultConfiguration();
            var faultyQuery = new GenericSelectQuery<int>("select count(*) from table_which_does_not_exist;");
            var handler = new SuppressingMySqlExceptionHandler();
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
            var faultyQuery = new GenericSelectQuery<int>(
                "select count(*) from table_which_does_not_exist;"
            );
            var handler = new SuppressingMySqlExceptionHandler();
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

        [Test]
        public void ShouldRespectNoSnakeCaseOnRequest()
        {
            // Arrange
            GlobalSetup.PerformDefaultConfiguration();
            using var _ = new AutoResetter(DisableSnakeCase, EnableSnakeCase);
            using var conn = GlobalSetup.ConnectToTempDb();
            CreateCaseTestingTableOn(conn);
            var titleCaseValue = GetRandomString(4);
            var snakeCaseValue = GetAnother(titleCaseValue, () => GetRandomString(4));
            var id = CommandExecutor.Execute(
                new CreateCaseTestingEntity(
                    titleCaseValue,
                    snakeCaseValue
                )
            );
            var query = new FindCaseTestingEntity(id);

            // Act
            var whenSnakeCaseDisabled = QueryExecutor.Execute(query);
            EnableSnakeCase();
            var whenSnakeCaseEnabled = QueryExecutor.Execute(query);
            
            // Assert
            Expect(whenSnakeCaseDisabled)
                .Not.To.Be.Null("entity not found at all");
            Expect(whenSnakeCaseDisabled.TitleCased)
                .To.Equal(titleCaseValue, "TitleCased should always be mapped");
            Expect(whenSnakeCaseDisabled.SnakeCased)
                .To.Be.Null("snake_case is disabled -- should not map the property");
            
            Expect(whenSnakeCaseEnabled)
                .Not.To.Be.Null("entity not found at all");
            Expect(whenSnakeCaseEnabled.TitleCased)
                .To.Equal(titleCaseValue, "TitleCased should always be mapped");
            Expect(whenSnakeCaseEnabled.SnakeCased)
                .To.Equal(snakeCaseValue, "snake_case is enabled -- should be mapped");
        }

        private void CreateCaseTestingTableOn(IDbConnection conn)
        {
            conn.Execute(@"
            create table case_testing (
                id int auto_increment primary key, 
                TitleCased text,
                snake_cased text
            );");
        }

        public class CaseTesting : IEntity
        {
            public string TitleCased { get; set; }
            public string SnakeCased { get; set; }
        }

        public class FindCaseTestingEntity : Query<CaseTesting>
        {
            public int Id { get; }

            public FindCaseTestingEntity(
                int id
            )
            {
                Id = id;
            }

            public override void Execute()
            {
                Result = SelectFirst<CaseTesting>(
                    "select * from case_testing where id = @id;",
                    new { Id }
                );
            }

            public override void Validate()
            {
            }
        }

        public class CreateCaseTestingEntity : Command<int>
        {
            public string TitleCase { get; }
            public string SnakeCase { get; }

            public CreateCaseTestingEntity(
                string titleCase,
                string snakeCase)
            {
                TitleCase = titleCase;
                SnakeCase = snakeCase;
            }

            public override void Execute()
            {
                Result = SelectFirst<int>(@"
                insert into case_testing (TitleCased, snake_cased)
                values (@TitleCase, @SnakeCase);
                select LAST_INSERT_ID();
", new { TitleCase, SnakeCase });
            }

            public override void Validate()
            {
            }
        }


        private void EnableSnakeCase()
        {
            Fluently.Configure().WithSnakeCaseMappingEnabled();
        }

        private void DisableSnakeCase()
        {
            Fluently.Configure().WithSnakeCaseMappingDisabled();
        }


        private static readonly ICache NoCache = new NoCache();

        private static readonly IQueryExecutor QueryExecutor =
            new QueryExecutor(NoCache);

        private static readonly ICommandExecutor CommandExecutor =
            new CommandExecutor(QueryExecutor, NoCache);
    }

    public class GenericSelectQuery<T> : SelectQuery<T>
    {
        public GenericSelectQuery(string sql) : base(sql)
        {
        }

        public override void Validate()
        {
        }
    }
}