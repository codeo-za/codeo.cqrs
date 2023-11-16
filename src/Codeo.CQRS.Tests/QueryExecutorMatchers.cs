using Codeo.CQRS.Caching;
using NExpect.Implementations;
using NExpect.Interfaces;
using NExpect.MatcherLogic;

namespace Codeo.CQRS.Tests
{
    public static class QueryExecutorMatchers
    {
        public static IMore<IQueryExecutor> MemoryCache(
            this IHave<IQueryExecutor> have
        )
        {
            return have.AddMatcher(actual =>
            {
                if (actual is not QueryExecutor qe)
                {
                    return new EnforcedMatcherResult(
                        false,
                        () => "Can only assert against the cache of a real QueryExecutor instance"
                    );
                }

                var cacheImplementation = qe.CurrentCache;
                var passed = cacheImplementation is MemoryCache;
                return new MatcherResult(
                    passed,
                    () => $"Expected {passed.AsNot()}to find MemoryCache"
                );
            });
        }
    }
}