using System;
using System.Linq.Expressions;
using NSubstitute;

namespace Codeo.CQRS;

public static class SubstituteQueryExecutorMockingExtensions
{
    public static IQueryExecutor WithMocked<TQuery, TResult>(
        this IQueryExecutor queryExecutor,
        TResult result
    )
        where TQuery : Query<TResult>
    {
        return queryExecutor.WithMocked<TQuery, TResult>(
            _ => true,
            _ => result
        );
    }

    public static IQueryExecutor WithMocked<TQuery, TResult>(
        this IQueryExecutor queryExecutor,
        Expression<Predicate<TQuery>> argsPredicate,
        TResult result
    )
        where TQuery : Query<TResult>
    {
        return queryExecutor.WithMocked(
            argsPredicate,
            _ => result
        );
    }

    public static IQueryExecutor WithMocked<TQuery, TResult>(
        this IQueryExecutor queryExecutor,
        Func<TQuery, TResult> handler
    ) where TQuery : Query<TResult>
    {
        return queryExecutor.WithMocked(
            q => true,
            handler
        );
    }

    public static IQueryExecutor WithMocked<TQuery, TResult>(
        this IQueryExecutor queryExecutor,
        Expression<Predicate<TQuery>> argsPredicate,
        Func<TQuery, TResult> handler
    )
        where TQuery : Query<TResult>
    {
        queryExecutor.Execute(Arg.Is(argsPredicate))
            .Returns(
                ci =>
                {
                    var qry = ci.Arg<TQuery>();
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
        this Query<T> query,
        T result
    )
    {
        query.SetResult(result);
        return result;
    }
}