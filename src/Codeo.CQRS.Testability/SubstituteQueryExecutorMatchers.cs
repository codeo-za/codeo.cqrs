using System;
using System.Linq;
using Codeo.CQRS.Internal;
using NExpect.Interfaces;
using NExpect.MatcherLogic;
using NSubstitute;
using static NExpect.Implementations.MessageHelpers;

namespace Codeo.CQRS;

/// <summary>
/// Provides NExpect matchers to make assertions against
/// the query executor just a little easier to write
/// </summary>
public static class SubstituteQueryExecutorMatchers
{
    /// <summary>
    /// Verify that the query executor executed any instance
    /// of TQuery - most useful in negation, ie
    /// Expect(queryExecutor)
    ///   .Not.To.Have.Executed&lt;SomeQuery&gt;();
    /// </summary>
    /// <param name="have"></param>
    /// <typeparam name="TQuery"></typeparam>
    /// <returns></returns>
    public static IMore<IQueryExecutor> Executed<TQuery>(
        this IHave<IQueryExecutor> have
    ) where TQuery : class, IQuery
    {
        return have.Executed<TQuery>(
            times: 1,
            _ => true
        );
    }

    /// <summary>
    /// Verify that the query executor executed any instance
    /// of TQuery the given number of times - most useful
    /// if the query has no identifying properties to
    /// discriminate by.
    /// </summary>
    /// <param name="have"></param>
    /// <param name="times"></param>
    /// <typeparam name="TQuery"></typeparam>
    /// <returns></returns>
    public static IMore<IQueryExecutor> Executed<TQuery>(
        this IHave<IQueryExecutor> have,
        int times
    ) where TQuery : class, IQuery
    {
        return have.Executed<TQuery>(
            times,
            _ => true
        );
    }

    /// <summary>
    /// Verifies that the query executor executed queries
    /// of the given type, matching the provided matcher,
    /// exactly once
    /// </summary>
    /// <param name="have"></param>
    /// <param name="matcher"></param>
    /// <typeparam name="TQuery"></typeparam>
    /// <returns></returns>
    public static IMore<IQueryExecutor> Executed<TQuery>(
        this IHave<IQueryExecutor> have,
        Func<TQuery, bool> matcher
    ) where TQuery : class, IQuery
    {
        return have.Executed(
            matcher,
            NULL_STRING
        );
    }

    /// <summary>
    /// Verifies that the query executor executed queries
    /// of the given type, matching the provided matcher,
    /// exactly once
    /// </summary>
    /// <param name="have"></param>
    /// <param name="matcher"></param>
    /// <param name="customMessage"></param>
    /// <typeparam name="TQuery"></typeparam>
    /// <returns></returns>
    public static IMore<IQueryExecutor> Executed<TQuery>(
        this IHave<IQueryExecutor> have,
        Func<TQuery, bool> matcher,
        string customMessage
    ) where TQuery : class, IQuery
    {
        return have.Executed(
            matcher,
            () => customMessage
        );
    }

    /// <summary>
    /// Verifies that the query executor executed queries
    /// of the given type, matching the provided matcher,
    /// exactly once
    /// </summary>
    /// <param name="have"></param>
    /// <param name="matcher"></param>
    /// <param name="customMessageGenerator"></param>
    /// <typeparam name="TQuery"></typeparam>
    /// <returns></returns>
    public static IMore<IQueryExecutor> Executed<TQuery>(
        this IHave<IQueryExecutor> have,
        Func<TQuery, bool> matcher,
        Func<string> customMessageGenerator
    ) where TQuery : class, IQuery
    {
        return have.Executed(
            1,
            matcher,
            customMessageGenerator
        );
    }

    /// <summary>
    /// Verifies that the query executor executed one query
    /// of the given type, matching the provided matcher,
    /// the required number of times
    /// </summary>
    /// <param name="have"></param>
    /// <param name="times"></param>
    /// <param name="matcher"></param>
    /// <typeparam name="TQuery"></typeparam>
    /// <returns></returns>
    public static IMore<IQueryExecutor> Executed<TQuery>(
        this IHave<IQueryExecutor> have,
        int times,
        Func<TQuery, bool> matcher
    ) where TQuery : class, IQuery
    {
        return have.Executed(
            times,
            matcher,
            NULL_STRING
        );
    }

    /// <summary>
    /// Verifies that the query executor executed one query
    /// of the given type, matching the provided matcher,
    /// the required number of times
    /// </summary>
    /// <param name="have"></param>
    /// <param name="times"></param>
    /// <param name="matcher"></param>
    /// <param name="customMessage"></param>
    /// <typeparam name="TQuery"></typeparam>
    /// <returns></returns>
    public static IMore<IQueryExecutor> Executed<TQuery>(
        this IHave<IQueryExecutor> have,
        int times,
        Func<TQuery, bool> matcher,
        string customMessage
    ) where TQuery : class, IQuery
    {
        return have.Executed(
            times,
            matcher,
            () => customMessage
        );
    }


    /// <summary>
    /// Verifies that the query executor executed one query
    /// of the given type, matching the provided matcher,
    /// the required number of times
    /// </summary>
    /// <param name="have"></param>
    /// <param name="times"></param>
    /// <param name="matcher"></param>
    /// <param name="customMessageGenerator"></param>
    /// <typeparam name="TQuery"></typeparam>
    /// <returns></returns>
    public static IMore<IQueryExecutor> Executed<TQuery>(
        this IHave<IQueryExecutor> have,
        int times,
        Func<TQuery, bool> matcher,
        Func<string> customMessageGenerator
    ) where TQuery : class, IQuery
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

                var allQueriesOfType = actual.ReceivedCalls()
                    .Where(ci => ci.GetMethodInfo().Name == nameof(IQueryExecutor.Execute))
                    .Select(ci => ci.GetArguments()[0] as TQuery)
                    .Where(q => q is not null)
                    .ToArray();
                var matches = allQueriesOfType.Where(matcher)
                    .ToArray();
                var passed = matches.Length == times;
                var queryTypeName = typeof(TQuery).PrettyName();
                var s = allQueriesOfType.Length == 1
                    ? ""
                    : "s";
                var moreInfo = allQueriesOfType.Length == 0
                    ? $"no executions of {queryTypeName} were observed"
                    : $"{allQueriesOfType.Length} execution{s} of {queryTypeName} where observed:\n{Dump(allQueriesOfType)}";

                return new MatcherResult(
                    passed,
                    FinalMessageFor(
                        () => $@"Expected {
                            passed.AsNot()
                        }to have received {
                            times
                        } executions of {
                            typeof(TQuery).PrettyName()
                        }{moreInfo}",
                        customMessageGenerator
                    )
                );
            }
        );
    }

    private static string Dump<TQuery>(
        TQuery[] allQueriesOfType
    ) where TQuery : class, IQuery
    {
        return allQueriesOfType
            .Select(o => o.Stringify())
            .AsTextList();
    }
}