using System;
using System.Net.Sockets;
using System.Transactions;
using Codeo.CQRS.Caching;
using Codeo.CQRS.Exceptions;
using MySql.Data.MySqlClient;
using NExpect;
using NUnit.Framework;
using static NExpect.Expectations;
using static PeanutButter.RandomGenerators.RandomValueGen;

namespace Codeo.CQRS.Tests
{
    [TestFixture]
    public class TestCommandExecution : TestFixtureRequiringData
    {
        [TestFixture]
        public class TransactionCompletedHandler
        {
            [Test]
            public void WhenNoTransactionExists_AndTransactionEventHandlerUsed_ShouldThrow()
            {
                // arrange
                var sut = Create();

                // act
                // assert
                Expect(() => sut.OnTransactionCompleted((e) =>
                    {
                    }))
                    .To.Throw("No ambient transaction scope exists");
            }

            [Test]
            public void WhenTransactionExists_AndTransactionHandlerUsed_AndTransactionRollback_ShouldInvokeHandler()
            {
                // arrange
                var sut = Create();
                TransactionStatus? transactionStatus = null;

                // act
                using (TransactionScopes.ReadCommitted())
                {
                    sut.OnTransactionCompleted(
                        e => transactionStatus = e.Transaction.TransactionInformation.Status);
                }

                // assert
                Expect(transactionStatus).To.Equal(TransactionStatus.Aborted);
            }

            [Test]
            public void WhenTransactionExists_AndTransactionHandlerUsed_AndTransactionCommitted_ShouldInvokeHandler()
            {
                // arrange
                var sut = Create();
                TransactionStatus? transactionStatus = null;

                // act
                using (var scope = TransactionScopes.ReadCommitted())
                {
                    sut.OnTransactionCompleted(
                        e => transactionStatus = e.Transaction.TransactionInformation.Status);

                    scope.Complete();
                }

                // assert
                Expect(transactionStatus).To.Equal(TransactionStatus.Committed);
            }

            private static Command Create()
            {
                return new TestCommand();
            }

            private class TestCommand : Command
            {
                public override void Execute()
                {
                }
            }
        }

        [TestFixture]
        public class AdvancedExecution
        {
            // required for code like Voucher's Distributed Lock

            [Test]
            public void ShouldBeAbleToSpecifyCommandTimeout()
            {
                // Arrange
                var cmd = Create();
                var threwSocketException = false;
                // Act
                try
                {
                    CommandExecutor.Execute(cmd);
                }
                catch (MySqlException ex)
                {
                    Exception current = ex;
                    while ((current = current.InnerException) != null)
                    {
                        if (current is SocketException)
                        {
                            threwSocketException = true;
                            break;
                        }
                    }
                }

                // Assert
                Expect(threwSocketException).To.Be.True();
            }

            [Test]
            public void ShouldBeAbleToProviderOnceOffExceptionHandler()
            {
                // Arrange
                var handler = new CustomHandler();
                var cmd = Create(handler);
                // Act
                Expect(() => CommandExecutor.Execute(cmd))
                    .Not.To.Throw();
                // Assert
                Expect(handler.CaughtException)
                    .Not.To.Be.Null();
                Expect(handler.HandledOperation)
                    .To.Equal(Operation.Insert);
            }

            private static ShouldTimeout Create(
                IExceptionHandler<MySqlException> customExceptionHandler = null
            )
            {
                return customExceptionHandler == null
                    ? new ShouldTimeout()
                    : new ShouldTimeout(customExceptionHandler);
            }


            public class CustomHandler : IExceptionHandler<MySqlException>
            {
                public MySqlException CaughtException { get; set; }
                public Operation? HandledOperation { get; set; }

                public ExceptionHandlingStrategy Handle(
                    Operation operation,
                    MySqlException exception)
                {
                    HandledOperation = operation;
                    CaughtException = exception;
                    return ExceptionHandlingStrategy.Suppress;
                }
            }

            public class ShouldTimeout : Command<int>
            {
                private readonly IExceptionHandler<MySqlException> _customHandler;

                public ShouldTimeout()
                {
                }

                public ShouldTimeout(IExceptionHandler<MySqlException> customHandler)
                {
                    _customHandler = customHandler;
                }

                public override void Execute()
                {
                    Result = Execute(Operation.Insert, "select sleep(2);", null, 1, _customHandler);
                }
            }
        }

        [TestFixture]
        public class DDLExecution: TestFixtureRequiringData
        {
            [Test]
            public void ShouldBeAbleToCreateAlterAndDropTable()
            {
                // Arrange
                Command create = new CreatesATable(),
                    alter = new AltersATable(),
                    drop = new DropsATable(),
                    insertShort = new InsertsAShortRow(),
                    insertLong = new InsertsALongRow();
                var countRows = new CountsRows();
                // Act
                Expect(() => CommandExecutor.Execute(create))
                    .Not.To.Throw("Should be able to create the table");
                Expect(() => CommandExecutor.Execute(insertShort))
                    .Not.To.Throw("Should be able to insert a short row");
                Expect(QueryExecutor.Execute(countRows))
                    .To.Equal(1, "Should have one row");
                Expect(() => CommandExecutor.Execute(insertLong))
                    .To.Throw("Should not be able to insert a long row yet");
                Expect(() => CommandExecutor.Execute(alter))
                    .Not.To.Throw("Should be able to alter table");
                Expect(() => CommandExecutor.Execute(insertShort))
                    .Not.To.Throw("Short insert should still work");
                Expect(() => CommandExecutor.Execute(insertLong))
                    .Not.To.Throw("Should be able to insert long after alter");
                Expect(QueryExecutor.Execute(countRows))
                    .To.Equal(3, "Should have 3 rows");
                Expect(() => CommandExecutor.Execute(drop))
                    .Not.To.Throw("Should be able to drop the table");
                Expect(() => QueryExecutor.Execute(countRows))
                    .To.Throw("Count should throw after table has been dropped");
                // Assert
            }

            public class InsertsAShortRow : InsertCommand
            {
                public static string ShortLabel =>
                    GetRandomString(10, 16);

                public InsertsAShortRow()
                    : base($"insert into ddl_execute_test (label) values ('{ShortLabel}');")
                {
                }
            }

            public class InsertsALongRow : InsertCommand
            {
                public static string ShortLabel =>
                    GetRandomString(17, 32);

                public InsertsALongRow()
                    : base($"insert into ddl_execute_test (label) values ('{ShortLabel}');")
                {
                }
            }

            public class CountsRows : SelectQuery<int>
            {
                public CountsRows() 
                    : base("select count(*) from ddl_execute_test;")
                {
                }
            }

            public class CreatesATable
                : Command
            {
                public override void Execute()
                {
                    ExecuteDdl(@"
drop table if exists ddl_execute_test; 
create table ddl_execute_test(id int primary key auto_increment, label varchar(16) not null);
");
                }
            }

            public class AltersATable : Command
            {
                public override void Execute()
                {
                    ExecuteDdl(@"
alter table ddl_execute_test change column label label varchar(32) not null;
");
                }
            }

            public class DropsATable : Command
            {
                public override void Execute()
                {
                    ExecuteDdl("drop table ddl_execute_test;");
                }
            }
        }

        private static readonly ICache NoCache = new NoCache();

        private static readonly IQueryExecutor QueryExecutor
            = new QueryExecutor(
                NoCache
            );

        private static readonly ICommandExecutor CommandExecutor
            = new CommandExecutor(
                QueryExecutor,
                NoCache
            );
    }
}