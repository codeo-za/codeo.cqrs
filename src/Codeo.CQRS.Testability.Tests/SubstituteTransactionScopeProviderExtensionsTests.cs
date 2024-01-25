using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using NSubstitute;

namespace Codeo.CQRS.Testability.Tests;

[TestFixture]
public class SubstituteTransactionScopeProviderExtensionsTests
{
    [TestFixture]
    public class WithScope
    {
        [TestFixture]
        public class GivenScopeOnly
        {
            public static IEnumerable<string> ListTransactionFactoryMethodsWithIsolationLevelInName()
            {
                var skip = new[]
                {
                    nameof(ITransactionScopeProvider.Suppress),
                    nameof(ITransactionScopeProvider.JoinOrDefault)
                };

                var result = typeof(ITransactionScopeProvider)
                    .GetMethods()
                    .Where(
                        mi => mi.ReturnType == typeof(ITransactionScope) &&
                            !skip.Contains(mi.Name)
                    )
                    .Select(mi => mi.Name);
                return result;
            }

            [TestCaseSource(nameof(ListTransactionFactoryMethodsWithIsolationLevelInName))]
            [Test]
            public void ShouldSetScopeReturnedBy_(string methodName)
            {
                // Arrange
                var method = typeof(ITransactionScopeProvider)
                    .GetMethod(methodName) ?? throw new Exception(
                    $"Can't fetch method info for '{methodName}'"
                );
                var option = GetRandom<TransactionScopeOption>();
                var timeout = GetRandomInt();
                var expected = Substitute.For<ITransactionScope>();
                // Act
                var provider = Substitute.For<ITransactionScopeProvider>()
                    .WithScope(expected);
                var result = method.Invoke(provider, [option, timeout]);
                // Assert
                Expect(result)
                    .To.Be(expected);
            }
        }

        [TestFixture]
        public class GivenScopeAndIsolationLevel
        {
            [Test]
            public void ShouldOnlyReturnThatScopeForTheSpecifiedIsolationLevel()
            {
                // Arrange
                var allIsolationLevels = Enum.GetValues<IsolationLevel>();
                var selected = GetRandomFrom(allIsolationLevels);
                var unselected = allIsolationLevels.Except(new[] { selected }).ToArray();
                var expected = Substitute.For<ITransactionScope>();
                // Act
                var provider = Substitute.For<ITransactionScopeProvider>()
                    .WithScope(expected, selected);
                var result = provider.JoinOrDefault(selected);
                var others = unselected.Select(
                    o => provider.JoinOrDefault(o)
                ).ToArray();
                // Assert
                Expect(result)
                    .To.Be(expected);
                Expect(others)
                    .Not.To.Contain(expected);
            }
        }

        [TestFixture]
        public class GivenFactory
        {
            [Test]
            public void ShouldProvideScopeFromFactory()
            {
                // Arrange
                var scopes = new Dictionary<IsolationLevel, ITransactionScope>()
                {
                    [IsolationLevel.ReadCommitted] = Substitute.For<ITransactionScope>(),
                    [IsolationLevel.RepeatableRead] = Substitute.For<ITransactionScope>()
                };

                // Act
                var provider = Substitute.For<ITransactionScopeProvider>()
                    .WithScope(
                        isolationLevel => scopes.TryGetValue(isolationLevel, out var scope)
                            ? scope
                            : Substitute.For<ITransactionScope>()
                    );
                var result1 = provider.JoinOrDefault(IsolationLevel.ReadCommitted);
                var result2 = provider.JoinOrDefault(IsolationLevel.RepeatableRead);
                var result3 = provider.JoinOrDefault(IsolationLevel.Snapshot);
                var result4 = provider.JoinOrDefault(IsolationLevel.Chaos);
                    
                // Assert
                Expect(result1)
                    .To.Be(scopes[IsolationLevel.ReadCommitted]);
                Expect(result2)
                    .To.Be(scopes[IsolationLevel.RepeatableRead]);
                Expect(scopes.Values)
                    .Not.To.Contain(result3);
                Expect(scopes.Values)
                    .Not.To.Contain(result4);
            }
        }
    }

    [TestFixture]
    public class WithSnapshotScope
    {
        [Test]
        public void ShouldReturnScopeForSnapshotOnly()
        {
            // Arrange
            var expected = GetRandom<ITransactionScope>();

            // Act
            var provider = Substitute.For<ITransactionScopeProvider>()
                .WithSnapshotScope(expected);
            var result = provider.Snapshot(
                GetRandom<TransactionScopeOption>(),
                GetRandom<int>()
            );
            // Assert
            Expect(result)
                .To.Be(expected);
            var others = new[]
            {
                provider.Suppress(),
                provider.ReadUncommitted(),
                provider.ReadCommitted(),
                provider.RepeatableRead(),
                provider.Serializable(),
                provider.JoinOrDefault(GetRandom<IsolationLevel>())
            };
            Expect(others)
                .Not.To.Contain(expected);
        }
    }

    [TestFixture]
    public class WithSerializableScope
    {
        [Test]
        public void ShouldReturnScopeForSerializableOnly()
        {
            // Arrange
            var expected = GetRandom<ITransactionScope>();

            // Act
            var provider = Substitute.For<ITransactionScopeProvider>()
                .WithSerializableScope(expected);
            var result = provider.Serializable(
                GetRandom<TransactionScopeOption>(),
                GetRandom<int>()
            );
            // Assert
            Expect(result)
                .To.Be(expected);
            var others = new[]
            {
                provider.Suppress(),
                provider.ReadUncommitted(),
                provider.ReadCommitted(),
                provider.RepeatableRead(),
                provider.Snapshot(),
                provider.JoinOrDefault(GetRandom<IsolationLevel>())
            };
            Expect(others)
                .Not.To.Contain(expected);
        }
    }

    [TestFixture]
    public class WithRepeatableReadScope
    {
        [Test]
        public void ShouldReturnScopeForRepeatableReadOnly()
        {
            // Arrange
            var expected = GetRandom<ITransactionScope>();

            // Act
            var provider = Substitute.For<ITransactionScopeProvider>()
                .WithRepeatableReadScope(expected);
            var result = provider.RepeatableRead(
                GetRandom<TransactionScopeOption>(),
                GetRandom<int>()
            );
            // Assert
            Expect(result)
                .To.Be(expected);
            var others = new[]
            {
                provider.Suppress(),
                provider.ReadUncommitted(),
                provider.ReadCommitted(),
                provider.Serializable(),
                provider.Snapshot(),
                provider.JoinOrDefault(GetRandom<IsolationLevel>())
            };
            Expect(others)
                .Not.To.Contain(expected);
        }
    }

    [TestFixture]
    public class WithReadCommittedScope
    {
        [Test]
        public void ShouldReturnScopeForReadCommittedOnly()
        {
            // Arrange
            var expected = GetRandom<ITransactionScope>();

            // Act
            var provider = Substitute.For<ITransactionScopeProvider>()
                .WithReadCommittedScope(expected);
            var result = provider.ReadCommitted(
                GetRandom<TransactionScopeOption>(),
                GetRandom<int>()
            );
            // Assert
            Expect(result)
                .To.Be(expected);
            var others = new[]
            {
                provider.Suppress(),
                provider.ReadUncommitted(),
                provider.RepeatableRead(),
                provider.Serializable(),
                provider.Snapshot(),
                provider.JoinOrDefault(GetRandom<IsolationLevel>())
            };
            Expect(others)
                .Not.To.Contain(expected);
        }
    }

    [TestFixture]
    public class WithReadUncommittedScope
    {
        [Test]
        public void ShouldReturnScopeForReadUncommittedOnly()
        {
            // Arrange
            var expected = GetRandom<ITransactionScope>();

            // Act
            var provider = Substitute.For<ITransactionScopeProvider>()
                .WithReadUncommittedScope(expected);
            var result = provider.ReadUncommitted(
                GetRandom<TransactionScopeOption>(),
                GetRandom<int>()
            );
            // Assert
            Expect(result)
                .To.Be(expected);
            var others = new[]
            {
                provider.Suppress(),
                provider.ReadCommitted(),
                provider.RepeatableRead(),
                provider.Serializable(),
                provider.Snapshot(),
                provider.JoinOrDefault(GetRandom<IsolationLevel>())
            };
            Expect(others)
                .Not.To.Contain(expected);
        }
    }

    [TestFixture]
    public class WithSuppressedScope
    {
        [Test]
        public void ShouldReturnScopeForSuppressedOnly()
        {
            // Arrange
            var expected = GetRandom<ITransactionScope>();

            // Act
            var provider = Substitute.For<ITransactionScopeProvider>()
                .WithSuppressedScope(expected);
            var result = provider.Suppress(
                GetRandom<int>()
            );
            // Assert
            Expect(result)
                .To.Be(expected);
            var others = new[]
            {
                provider.ReadUncommitted(),
                provider.ReadCommitted(),
                provider.RepeatableRead(),
                provider.Serializable(),
                provider.Snapshot(),
                provider.JoinOrDefault(GetRandom<IsolationLevel>())
            };
            Expect(others)
                .Not.To.Contain(expected);
        }
    }
}