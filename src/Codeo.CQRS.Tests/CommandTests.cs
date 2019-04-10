using NExpect;
using NUnit.Framework;
using static NExpect.Expectations;

namespace Codeo.CQRS.Tests
{
    [TestFixture]
    public class CommandTests
    {
        [TestFixture]
        public class TransactionCompletedHandler : CommandTests
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
            public void WhenTransactionExists_AndTransactionHandlerUsed_AnyTransactionRollback_ShouldntInvokeHandler()
            {
                // arrange
                var sut = this.Create();
                var handlerInvoked = false;

                // act
                using (var scope = TransactionScopes.ReadCommitted())
                {
                    sut.OnTransactionCompleted(e => handlerInvoked = true);
                }
                
                // assert
                Expect(handlerInvoked).To.Be.False();
            }
            
            [Test]
            public void WhenTransactionExists_AndTransactionHandlerUsed_AndTransactionCommitted_ShouldInvokeHandler()
            {
                // arrange
                var sut = this.Create();
                var handlerInvoked = false;

                // act
                using (var scope = TransactionScopes.ReadCommitted())
                {
                    sut.OnTransactionCompleted(e => handlerInvoked = true);
                    scope.Complete();
                }
                
                // assert
                Expect(handlerInvoked).To.Be.True();
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