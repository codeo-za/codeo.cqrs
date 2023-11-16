using System.Transactions;
using NUnit.Framework;
// ReSharper disable MemberCanBePrivate.Global

namespace Codeo.CQRS.Tests
{
    public abstract class TestFixtureRequiringTransaction : TestFixtureRequiringData
    {
        [SetUp]
        public void TestFixtureRequiringDataSetup()
        {
            StartTransaction();
        }

        [TearDown]
        public void TestFixtureRequiringDataTeardown()
        {
            RollbackTransaction();
        }

    }

    public abstract class TestFixtureRequiringData
    {
        private ITransactionScope _tx;

        protected void StartTransaction()
        {
            Transaction.Current?.Dispose();
            _tx = TransactionScopes.ReadCommitted();
        }

        protected void RollbackTransaction()
        {
            _tx?.Dispose();
            _tx = null;
        }

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            GlobalSetup.RunOneTimeSetupIfRequired();
        }
    }
}