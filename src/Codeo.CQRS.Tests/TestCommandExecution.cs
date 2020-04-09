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
        public class TransactionCompletedHandler : TestCommandExecution
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
        }

        [TestFixture]
        public class AdvancedExecution : TestCommandExecution
        {
            // required for code like Voucher's Distributed Lock
            [Test]
            public void ShouldBeAbleToSpecifyCommandTimeout()
            {
                // Arrange
                var cmd = new ShouldTimeout();
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
                var cmd = new ShouldTimeout(handler);
                // Act
                Expect(() => CommandExecutor.Execute(cmd))
                    .Not.To.Throw();
                // Assert
                Expect(handler.CaughtException)
                    .Not.To.Be.Null();
                Expect(handler.HandledOperation)
                    .To.Equal(Operation.Insert);
            }

            private static readonly ICache NoCache = new NoCache();

            private static readonly ICommandExecutor CommandExecutor
                = new CommandExecutor(
                    new QueryExecutor(
                        NoCache
                    ), NoCache);

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
                    return ExceptionHandlingStrategy.Throw;
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
        }


        protected Command Create()
        {
            return new TestCommand();
        }

        public class TestCommand : Command
        {
            public override void Execute()
            {
            }
        }
    }
}