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
                var cache = new NoCache();
                var executor = new CommandExecutor(new QueryExecutor(cache), cache);
                var threwSocketException = false;
                // Act
                try
                {
                    executor.Execute(cmd);
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
        }

        public class ShouldTimeout : Command<int>
        {
            public override void Execute()
            {
                Result = Execute(Operation.Insert, "select sleep(2);", null, 1);
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