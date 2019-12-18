using System.Transactions;
using NExpect;
using NUnit.Framework;
using static NExpect.Expectations;

namespace Codeo.CQRS.Tests
{
    [TestFixture]
    public class TestCommandExecution: TestFixtureRequiringData
    {
        [TestFixture]
        public class TransactionCompletedHandler : TestCommandExecution
        {
            [Test]
            public void WhenNoTransactionExists_AndTransactionEventHandlerUsed_ShouldThrow()
            {
                // arrange
                var sut = this.Create();

                // act
                // assert
                Expect(() => sut.OnTransactionCompleted((e) => { }))
                    .To.Throw("No ambient transaction scope exists");
            }
            
            [Test]
            public void WhenTransactionExists_AndTransactionHandlerUsed_AndTransactionRollback_ShouldInvokeHandler()
            {
                // arrange
                var sut = this.Create();
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
                var sut = this.Create();
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