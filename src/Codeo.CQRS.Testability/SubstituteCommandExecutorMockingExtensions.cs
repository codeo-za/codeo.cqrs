using System;
using System.Linq.Expressions;
using NSubstitute;

namespace Codeo.CQRS;

public static class SubstituteCommandExecutorMockingExtensions
{
    public static ICommandExecutor WithMocked<TCommand>(
        this ICommandExecutor commandExecutor,
        Action<TCommand> commandLogic
    ) where TCommand : ICommand
    {
        return commandExecutor.WithMocked(
            o => true,
            commandLogic
        );
    }

    public static ICommandExecutor WithMocked<TCommand>(
        this ICommandExecutor commandExecutor,
        Expression<Predicate<TCommand>> argsPredicate,
        Action<TCommand> commandLogic
    ) where TCommand : ICommand
    {
        commandExecutor.When(
            o => o.Execute(Arg.Is(argsPredicate))
        ).Do(
            ci => commandLogic(ci.Arg<TCommand>())
        );
        return commandExecutor;
    }

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
        this ICommandExecutor commandExecutor,
        Expression<Predicate<TCommand>> argsPredicate,
        TResult result
    ) where TCommand : Command<TResult>
    {
        return commandExecutor.WithMocked(
            argsPredicate,
            _ => result
        );
    }

    public static ICommandExecutor WithMocked<TCommand, TResult>(
        this ICommandExecutor commandExecutor,
        Expression<Predicate<TCommand>> argsPredicate,
        Func<TCommand, TResult> handler
    )
        where TCommand : Command<TResult>
    {
        commandExecutor.Execute(Arg.Is(argsPredicate))
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
        return commandExecutor;
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