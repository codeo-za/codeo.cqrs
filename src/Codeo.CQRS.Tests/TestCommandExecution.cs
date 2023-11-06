using System;
using System.Net.Sockets;
using System.Transactions;
using Codeo.CQRS.Caching;
using Codeo.CQRS.Exceptions;
using MySql.Data.MySqlClient;
using NExpect;
using NUnit.Framework;
using static NExpect.Expectations;

namespace Codeo.CQRS.Tests
{
    [TestFixture]
    public class TestCommandExecution : TestFixtureRequiringData
    {
        [TestFixture]
        public class TransactionCompletedHandler : TestFixtureRequiringData
        {
            [Test]
            public void WhenNoTransactionExists_AndTransactionEventHandlerUsed_ShouldThrow()
            {
                // arrange
                var sut = CreateTestCommand();

                // act
                // assert
                Expect(
                        () => sut.OnTransactionCompleted(
                            (e) =>
                            {
                            }
                        )
                    )
                    .To.Throw("No ambient transaction scope exists");
            }

            [Test]
            public void WhenTransactionExists_AndTransactionHandlerUsed_AndTransactionRollback_ShouldInvokeHandler()
            {
                // arrange
                var sut = CreateTestCommand();
                TransactionStatus? transactionStatus = null;

                // act
                using (TransactionScopes.ReadCommitted())
                {
                    sut.OnTransactionCompleted(
                        e => transactionStatus = e.Transaction!.TransactionInformation.Status
                    );
                }

                // assert
                Expect(transactionStatus).To.Equal(TransactionStatus.Aborted);
            }

            [Test]
            public void WhenTransactionExists_AndTransactionHandlerUsed_AndTransactionCommitted_ShouldInvokeHandler()
            {
                // arrange
                var sut = CreateTestCommand();
                TransactionStatus? transactionStatus = null;

                // act
                using (var scope = TransactionScopes.ReadCommitted())
                {
                    sut.OnTransactionCompleted(
                        e => transactionStatus = e.Transaction!.TransactionInformation.Status
                    );

                    scope.Complete();
                }

                // assert
                Expect(transactionStatus).To.Equal(TransactionStatus.Committed);
            }
        }

        [TestFixture]
        public class AdvancedExecution : TestFixtureRequiringData
        {
            // required for code like Voucher's Distributed Lock
            [Test]
            public void ShouldBeAbleToSpecifyCommandTimeout()
            {
                // Arrange
                var sut = Create();
                var cmd = new ShouldTimeout();
                var threwSocketException = false;
                // Act
                try
                {
                    sut.Execute(cmd);
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
                var cmd = new ShouldTimeout(handler);
                var sut = Create();
                // Act
                Expect(() => sut.Execute(cmd))
                    .Not.To.Throw();
                // Assert
                Expect(handler.CaughtException)
                    .Not.To.Be.Null();
                Expect(handler.HandledOperation)
                    .To.Equal(Operation.Insert);
            }

            public class CustomHandler : IExceptionHandler<MySqlException>
            {
                public MySqlException CaughtException { get; set; }
                public Operation? HandledOperation { get; set; }

                public ExceptionHandlingStrategy Handle(
                    Operation operation,
                    MySqlException exception
                )
                {
                    HandledOperation = operation;
                    CaughtException = exception;
                    return ExceptionHandlingStrategy.Suppress;
                }
            }
        }

        [TestFixture]
        public class DerivedCommandsWhereBaseReturnsValue : TestFixtureRequiringData
        {
            [Test]
            public void ShouldExecuteToReturnValue()
            {
                // Arrange
                var cmd = new DerivedCommand();
                var sut = Create();
                // Act
                var result = sut.Execute(cmd);
                // Assert
                Expect(result)
                    .To.Equal(1);
            }

            public class DerivedCommand : BaseCommand
            {
            }

            public class BaseCommand : Command<int>
            {
                public override void Execute()
                {
                    Result = 1;
                }

                public override void Validate()
                {
                }
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

            public override void Validate()
            {
            }
        }

        private static CommandExecutor Create(
            IQueryExecutor queryExecutor = null,
            ICache cache = null
        )
        {
            cache ??= new NoCache();
            return new CommandExecutor(
                queryExecutor ?? new QueryExecutor(cache),
                cache
            );
        }

        protected static Command CreateTestCommand()
        {
            return new TestCommand();
        }

        public class TestCommand : Command
        {
            public override void Execute()
            {
            }

            public override void Validate()
            {
            }
        }
    }
}