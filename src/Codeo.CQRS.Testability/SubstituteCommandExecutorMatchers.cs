using System;
using System.Linq;
using Codeo.CQRS.Internal;
using NExpect.Interfaces;
using NExpect.MatcherLogic;
using NSubstitute;
using static NExpect.Implementations.MessageHelpers;

namespace Codeo.CQRS;

public static class SubstituteCommandExecutorMatchers
{
    /// <summary>
    /// Verify that the command executor executed any instance
    /// of TCommand - most useful in negation, ie
    /// Expect(commandExecutor)
    ///   .Not.To.Have.Executed&lt;SomeCommand&gt;();
    /// </summary>
    /// <param name="have"></param>
    /// <typeparam name="TCommand"></typeparam>
    /// <returns></returns>
    public static IMore<ICommandExecutor> Executed<TCommand>(
        this IHave<ICommandExecutor> have
    ) where TCommand : class, ICommand
    {
        return have.Executed<TCommand>(times: 1);
    }

    /// <summary>
    /// Verify that the command executor executed any instance
    /// of TCommand - useful when the command has no identifying
    /// properties, or to be followed up with other assertions
    /// </summary>
    /// <param name="have"></param>
    /// <param name="times"></param>
    /// <typeparam name="TCommand"></typeparam>
    /// <returns></returns>
    public static IMore<ICommandExecutor> Executed<TCommand>(
        this IHave<ICommandExecutor> have,
        int times
    ) where TCommand : class, ICommand
    {
        return have.Executed<TCommand>(
            times,
            _ => true
        );
    }

    /// <summary>
    /// Verify that the command executor executed a command
    /// of the given type, matching the given matcher,
    /// exactly once only
    /// </summary>
    /// <param name="have"></param>
    /// <param name="matcher"></param>
    /// <typeparam name="TCommand"></typeparam>
    /// <returns></returns>
    public static IMore<ICommandExecutor> Executed<TCommand>(
        this IHave<ICommandExecutor> have,
        Func<TCommand, bool> matcher
    ) where TCommand : class, ICommand
    {
        return have.Executed(
            matcher,
            NULL_STRING
        );
    }

    /// <summary>
    /// Verify that the command executor executed a command
    /// of the given type, matching the given matcher,
    /// exactly once only
    /// </summary>
    /// <param name="have"></param>
    /// <param name="matcher"></param>
    /// <param name="customMessage"></param>
    /// <typeparam name="TCommand"></typeparam>
    /// <returns></returns>
    public static IMore<ICommandExecutor> Executed<TCommand>(
        this IHave<ICommandExecutor> have,
        Func<TCommand, bool> matcher,
        string customMessage
    ) where TCommand : class, ICommand
    {
        return have.Executed(
            matcher,
            () => customMessage
        );
    }

    /// <summary>
    /// Verify that the command executor executed a command
    /// of the given type, matching the given matcher,
    /// exactly once only
    /// </summary>
    /// <param name="have"></param>
    /// <param name="matcher"></param>
    /// <param name="customMessageGenerator"></param>
    /// <typeparam name="TCommand"></typeparam>
    /// <returns></returns>
    public static IMore<ICommandExecutor> Executed<TCommand>(
        this IHave<ICommandExecutor> have,
        Func<TCommand, bool> matcher,
        Func<string> customMessageGenerator
    ) where TCommand : class, ICommand
    {
        return have.Executed(
            1,
            matcher,
            customMessageGenerator
        );
    }

    /// <summary>
    /// Verify that the command executor executed a command
    /// of the given type, matching the given matcher, the
    /// required number of times
    /// </summary>
    /// <param name="have"></param>
    /// <param name="times"></param>
    /// <param name="matcher"></param>
    /// <typeparam name="TCommand"></typeparam>
    /// <returns></returns>
    public static IMore<ICommandExecutor> Executed<TCommand>(
        this IHave<ICommandExecutor> have,
        int times,
        Func<TCommand, bool> matcher
    ) where TCommand : class, ICommand
    {
        return have.Executed(
            times,
            matcher,
            NULL_STRING
        );
    }

    /// <summary>
    /// Verify that the command executor executed a command
    /// of the given type, matching the given matcher, the
    /// required number of times
    /// </summary>
    /// <param name="have"></param>
    /// <param name="times"></param>
    /// <param name="matcher"></param>
    /// <param name="customMessage"></param>
    /// <typeparam name="TCommand"></typeparam>
    /// <returns></returns>
    public static IMore<ICommandExecutor> Executed<TCommand>(
        this IHave<ICommandExecutor> have,
        int times,
        Func<TCommand, bool> matcher,
        string customMessage
    ) where TCommand : class, ICommand
    {
        return have.Executed(
            times,
            matcher,
            () => customMessage
        );
    }

    /// <summary>
    /// Verify that the command executor executed a command
    /// of the given type, matching the given matcher, the
    /// required number of times
    /// </summary>
    /// <param name="have"></param>
    /// <param name="times"></param>
    /// <param name="matcher"></param>
    /// <param name="customMessageGenerator"></param>
    /// <typeparam name="TCommand"></typeparam>
    /// <returns></returns>
    public static IMore<ICommandExecutor> Executed<TCommand>(
        this IHave<ICommandExecutor> have,
        int times,
        Func<TCommand, bool> matcher,
        Func<string> customMessageGenerator
    ) where TCommand : class, ICommand
    {
        return have.AddMatcher(
            actual =>
            {
                if (actual is null)
                {
                    return new EnforcedMatcherResult(false, "queryExecutor is null");
                }

                if (times < 0)
                {
                    return new EnforcedMatcherResult(false, "expected times value is < 0");
                }

                var allCommandsOfType = actual.ReceivedCalls()
                    .Where(ci => ci.GetMethodInfo().Name == nameof(ICommandExecutor.Execute))
                    .Select(ci => ci.GetArguments()[0] as TCommand)
                    .Where(q => q is not null)
                    .ToArray();
                var matches = allCommandsOfType.Where(matcher)
                    .ToArray();
                var passed = matches.Length == times;
                var commandTypeName = typeof(TCommand).PrettyName();
                var s = allCommandsOfType.Length == 1
                    ? ""
                    : "s";
                var moreInfo = allCommandsOfType.Length == 0
                    ? $"no executions of {commandTypeName} were observed"
                    : $"{allCommandsOfType.Length} execution{s} of {commandTypeName} where observed:\n{Dump(allCommandsOfType)}";

                return new MatcherResult(
                    passed,
                    FinalMessageFor(
                        () => $@"Expected {
                            passed.AsNot()
                        }to have received {
                            times
                        } executions of {
                            typeof(TCommand).PrettyName()
                        }{moreInfo}",
                        customMessageGenerator
                    )
                );
            }
        );
    }

    private static string Dump<TCommand>(
        TCommand[] receivedCommands
    ) where TCommand : class, ICommand
    {
        return receivedCommands
            .Select(o => o.Stringify())
            .AsTextList();
    }
}