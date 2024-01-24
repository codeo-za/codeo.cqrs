using System;
using System.Transactions;
using NSubstitute;

// ReSharper disable MemberCanBePrivate.Global

namespace Codeo.CQRS;

public static class SubstituteTransactionScopeProviderExtensions
{
    public static ITransactionScopeProvider WithScope(
        this ITransactionScopeProvider provider,
        ITransactionScope scope
    )
    {
        return provider
            .WithSuppressedScope(scope)
            .WithReadUncommittedScope(scope)
            .WithReadCommittedScope(scope)
            .WithRepeatableReadScope(scope)
            .WithSerializableScope(scope)
            .WithSnapshotScope(scope)
            .WithScope(
                _ => scope
            );
    }

    public static ITransactionScopeProvider WithSnapshotScope(
        this ITransactionScopeProvider provider,
        ITransactionScope scope
    )
    {
        provider.Snapshot(Arg.Any<TransactionScopeOption>(), Arg.Any<int>())
            .Returns(scope);
        return provider;
    }

    public static ITransactionScopeProvider WithSerializableScope(
        this ITransactionScopeProvider provider,
        ITransactionScope scope
    )
    {
        provider.Serializable(Arg.Any<TransactionScopeOption>(), Arg.Any<int>())
            .Returns(scope);
        return provider;
    }

    public static ITransactionScopeProvider WithRepeatableReadScope(
        this ITransactionScopeProvider provider,
        ITransactionScope scope
    )
    {
        provider.RepeatableRead(Arg.Any<TransactionScopeOption>(), Arg.Any<int>())
            .Returns(scope);
        return provider;
    }

    public static ITransactionScopeProvider WithReadCommittedScope(
        this ITransactionScopeProvider provider,
        ITransactionScope scope
    )
    {
        provider.ReadCommitted(Arg.Any<TransactionScopeOption>(), Arg.Any<int>())
            .Returns(scope);
        return provider;
    }

    public static ITransactionScopeProvider WithReadUncommittedScope(
        this ITransactionScopeProvider provider,
        ITransactionScope scope
    )
    {
        provider.ReadUncommitted(Arg.Any<TransactionScopeOption>(), Arg.Any<int>())
            .Returns(scope);
        return provider;
    }

    public static ITransactionScopeProvider WithSuppressedScope(
        this ITransactionScopeProvider provider,
        ITransactionScope scope
    )
    {
        provider.Suppress(Arg.Any<int>()).Returns(scope);
        return provider;
    }

    public static ITransactionScopeProvider WithScope(
        this ITransactionScopeProvider provider,
        Func<IsolationLevel, ITransactionScope> factory
    )
    {
        provider.JoinOrDefault(Arg.Any<IsolationLevel>())
            .Returns(ci => factory(ci.Arg<IsolationLevel>()));
        return provider;
    }

    public static ITransactionScopeProvider WithScope(
        this ITransactionScopeProvider provider,
        ITransactionScope scope,
        IsolationLevel isolationLevel
    )
    {
        provider.JoinOrDefault(isolationLevel)
            .Returns(scope);
        return provider;
    }
}