using System.Linq;
using System.Text.RegularExpressions;
using NExpect.Interfaces;
using NExpect.MatcherLogic;

namespace Codeo.CQRS.Tests
{
    public static class SqlStringMatchers
    {
        public static IMore<string> Match(
            this ITo<string> to,
            string expected)
        {
            // sql testing shouldn't care about case or (too much) about
            // whitespace -- all whitespace is equal
            return to.AddMatcher(actual =>
            {
                var normalisedActual = Normalise(actual);
                var normalisedExpected = Normalise(expected);
                var passed = normalisedActual == normalisedExpected;
                return new MatcherResult(
                    passed,
                    () => $@"Expected
{actual}
to match (normalised)
{expected}

normalised values:
actual:
{normalisedActual}
expected
{normalisedExpected}"
                );
            });
        }

        private static readonly Regex WordBoundaries = new Regex("\\b");

        private static string Normalise(string sql)
        {
            return string.Join(
                " ",
                WordBoundaries.Split(sql)
                    .Select(w => w.Trim())
                    .Where(w => w != "")
                    .ToArray()
            ).ToLowerInvariant();
        }
    }
}