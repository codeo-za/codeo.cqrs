using System;
using System.Transactions;
using NUnit.Framework;
using NExpect;
using NExpect.Implementations;
using NExpect.Interfaces;
using NExpect.MatcherLogic;
using PeanutButter.Utils;
using static NExpect.Expectations;
using static PeanutButter.RandomGenerators.RandomValueGen;

namespace Codeo.CQRS.Tests
{
    [TestFixture]
    public class TransactionScopeProviderTests
    {
        [TestFixture]
        public class Suppress
        {
            [Test]
            public void ShouldSuppressTheCurrentTransaction()
            {
                // Arrange
                var sut = Create();
                // Act
                using var tx = new TransactionScope(TransactionScopeOption.Required, TimeSpan.FromSeconds(35));
                using var suppressed = sut.Suppress();

                // Assert
                Expect(Transaction.Current)
                    .To.Be.Null();
                suppressed.Dispose();
                Expect(Transaction.Current)
                    .Not.To.Be.Null();
            }
        }

        [TestFixture]
        public class ReadUncommitted
        {
            [Test]
            public void ShouldEstablishTransactionWithDefaultTimeout()
            {
                // Arrange
                var sut = Create();
                // Act
                using var _ = sut.ReadUncommitted();
                // Assert
                Expect(Transaction.Current?.IsolationLevel)
                    .To.Equal(IsolationLevel.ReadUncommitted);
                Expect(Transaction.Current)
                    .To.Have.Timeout(30);
            }

            [Test]
            public void ShouldEstablishTransactionWithProvidedTimeout()
            {
                // Arrange
                var timeout = GetRandomInt(20, 25);
                var sut = Create();
                // Act
                using var _ = sut.ReadUncommitted(timeout: timeout);
                // Assert
                Expect(Transaction.Current?.IsolationLevel)
                    .To.Equal(IsolationLevel.ReadUncommitted);
                Expect(Transaction.Current)
                    .To.Have.Timeout(timeout);
            }
        }

        [TestFixture]
        public class ReadCommitted
        {
            [Test]
            public void ShouldEstablishTransactionWithDefaultTimeout()
            {
                // Arrange
                var sut = Create();
                // Act
                using var _ = sut.ReadCommitted();
                // Assert
                Expect(Transaction.Current?.IsolationLevel)
                    .To.Equal(IsolationLevel.ReadCommitted);
                Expect(Transaction.Current)
                    .To.Have.Timeout(30);
            }

            [Test]
            public void ShouldEstablishTransactionWithProvidedTimeout()
            {
                // Arrange
                var timeout = GetRandomInt(20, 25);
                var sut = Create();
                // Act
                using var _ = sut.ReadCommitted(timeout: timeout);
                // Assert
                Expect(Transaction.Current?.IsolationLevel)
                    .To.Equal(IsolationLevel.ReadCommitted);
                Expect(Transaction.Current)
                    .To.Have.Timeout(timeout);
            }
        }

        [TestFixture]
        public class RepeatableRead
        {
            [Test]
            public void ShouldEstablishTransactionWithDefaultTimeout()
            {
                // Arrange
                var sut = Create();
                // Act
                using var _ = sut.RepeatableRead();
                // Assert
                Expect(Transaction.Current?.IsolationLevel)
                    .To.Equal(IsolationLevel.RepeatableRead);
                Expect(Transaction.Current)
                    .To.Have.Timeout(30);
            }

            [Test]
            public void ShouldEstablishTransactionWithProvidedTimeout()
            {
                // Arrange
                var timeout = GetRandomInt(20, 25);
                var sut = Create();
                // Act
                using var _ = sut.RepeatableRead(timeout: timeout);
                // Assert
                Expect(Transaction.Current?.IsolationLevel)
                    .To.Equal(IsolationLevel.RepeatableRead);
                Expect(Transaction.Current)
                    .To.Have.Timeout(timeout);
            }
        }

        [TestFixture]
        public class Serializable
        {
            [Test]
            public void ShouldEstablishTransactionWithDefaultTimeout()
            {
                // Arrange
                var sut = Create();
                // Act
                using var _ = sut.Serializable();
                // Assert
                Expect(Transaction.Current?.IsolationLevel)
                    .To.Equal(IsolationLevel.Serializable);
                Expect(Transaction.Current)
                    .To.Have.Timeout(30);
            }

            [Test]
            public void ShouldEstablishTransactionWithProvidedTimeout()
            {
                // Arrange
                var timeout = GetRandomInt(20, 25);
                var sut = Create();
                // Act
                using var _ = sut.Serializable(timeout: timeout);
                // Assert
                Expect(Transaction.Current?.IsolationLevel)
                    .To.Equal(IsolationLevel.Serializable);
                Expect(Transaction.Current)
                    .To.Have.Timeout(timeout);
            }
        }

        [TestFixture]
        public class Snapshot
        {
            [Test]
            public void ShouldEstablishTransactionWithDefaultTimeout()
            {
                // Arrange
                var sut = Create();
                // Act
                using var _ = sut.Snapshot();
                // Assert
                Expect(Transaction.Current?.IsolationLevel)
                    .To.Equal(IsolationLevel.Snapshot);
                Expect(Transaction.Current)
                    .To.Have.Timeout(30);
            }

            [Test]
            public void ShouldEstablishTransactionWithProvidedTimeout()
            {
                // Arrange
                var timeout = GetRandomInt(20, 25);
                var sut = Create();
                // Act
                using var _ = sut.Snapshot(timeout: timeout);
                // Assert
                Expect(Transaction.Current?.IsolationLevel)
                    .To.Equal(IsolationLevel.Snapshot);
                Expect(Transaction.Current)
                    .To.Have.Timeout(timeout);
            }

            [TestFixture]
            public class JoinOrDefault
            {
                [TestFixture]
                public class WhenNoExistingAmbientTransaction
                {
                    [Test]
                    public void ShouldStartNewTransaction()
                    {
                        // Arrange
                        var expected = GetRandomFrom(
                            new[]
                            {
                                IsolationLevel.Serializable,
                                IsolationLevel.ReadUncommitted,
                                IsolationLevel.ReadCommitted
                            });
                        var sut = Create();
                        Expect(Transaction.Current)
                            .To.Be.Null();
                        // Act
                        using var _ = sut.JoinOrDefault(expected);
                        // Assert
                        Expect(Transaction.Current?.IsolationLevel)
                            .To.Equal(expected);
                    }
                }
            }
        }

        [Test]
        public void TxTicksTranslations()
        {
            // Arrange
            var seconds = 25;
            // Act
            var txTix = TransactionMatchers.TransactionTimeoutTicks(
                TimeSpan.FromSeconds(seconds)
            );
            Expect(txTix)
                .To.Equal(50);
            var reverted = TransactionMatchers.TransactionTimeoutSeconds(txTix);
            Expect(reverted)
                .To.Equal(25);
            // Assert
        }

        private static ITransactionScopeProvider Create()
        {
            return new TransactionScopeProvider();
        }
    }

    public static class TransactionMatchers
    {
        public static IMore<Transaction> Timeout(
            this IHave<Transaction> have,
            int seconds
        )
        {
            return have.Timeout(TimeSpan.FromSeconds(seconds));
        }

        public static IMore<Transaction> Timeout(
            this IHave<Transaction> have,
            TimeSpan timeout
        )
        {
            return have.AddMatcher(actual =>
            {
                if (actual is null)
                {
                    return new EnforcedMatcherResult(
                        false,
                        () => "Cannot test timeout on null transaction"
                    );
                }

                var internalTx = actual.Get<object>("_internalTransaction");
                if (internalTx is null)
                {
                    return new EnforcedMatcherResult(false, () => "No internal transaction found");
                }

                var expected = TransactionTimeoutTicks(timeout);
                var absoluteTimeout = internalTx.Get<long>("AbsoluteTimeout");
                var passed = absoluteTimeout == expected;
                var txSeconds = TransactionTimeoutSeconds(absoluteTimeout);
                return new MatcherResult(
                    passed,
                    () => $"Expected transaction {passed.AsNot()}to have timeout {timeout}s, but found {txSeconds}s"
                );
            });
        }

        public static long TransactionTimeoutSeconds(long txTicks)
        {
            if (txTicks == long.MaxValue)
            {
                return 0;
            }

            // although this reverses the logic below, I think that was put in to cater
            // for rounding errors - keeping this in makes the ticks tests fail
            // txTicks -= 2;
            txTicks <<= TimerInternalExponent;
            txTicks *= TimeSpan.TicksPerMillisecond;
            return TimeSpan.FromTicks(txTicks).Seconds;
        }

        private const int TimerInternalExponent = 9;

        public static long TransactionTimeoutTicks(TimeSpan timeout)
        {
            // largely stolen by decompiling System.Transaction.Transactions:
            // the timeout for a transaction is stored as this "ticks" value
            // which are approximately 2 per second, but just in case there's
            // something funky with ticks on another machine, I'm keeping much
            // of it intact

            if (timeout != TimeSpan.Zero)
            {
                var timeoutTicks =
                    (timeout.Ticks / TimeSpan.TicksPerMillisecond) >>
                    TimerInternalExponent;
                return timeoutTicks + 2;
            }

            return long.MaxValue;
        }
    }
}