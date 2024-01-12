using System;
using System.Linq.Expressions;
using NSubstitute;

namespace Codeo.CQRS;

public static class SubstituteCommandExecutorExtensions
{
    public static ICommandExecutor WithMocked<TCommand, TResult>(
        this ICommandExecutor commandExecutor,
        TResult result
    ) where TCommand : Command<TResult>
    {
        return commandExecutor.WithMocked<TCommand, TResult>(
            o => true,
            q => result
        );
    }

    public static ICommandExecutor WithMocked<TCommand, TResult>(
        this ICommandExecutor queryExecutor,
        Expression<Predicate<TCommand>> argsPredicate,
        TResult result
    ) where TCommand : Command<TResult>
    {
        return queryExecutor.WithMocked(
            argsPredicate,
            _ => result
        );
    }

    public static ICommandExecutor WithMocked<TCommand, TResult>(
        this ICommandExecutor queryExecutor,
        Expression<Predicate<TCommand>> argsPredicate,
        Func<TCommand, TResult> handler
    )
        where TCommand : Command<TResult>
    {
        queryExecutor.Execute(Arg.Is(argsPredicate))
            .Returns(
                ci =>
                {
                    var qry = ci.Arg<TCommand>();
                    var result = handler(qry);
                    return AttachResult(
                        qry,
                        result
                    );
                }
            );
        return queryExecutor;
    }

    private static T AttachResult<T>(
        this Command<T> query,
        T result
    )
    {
        query.SetResult(result);
        return result;
    }
}