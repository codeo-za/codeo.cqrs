using System;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using static PeanutButter.RandomGenerators.RandomValueGen;
using NExpect;
using NExpect.Interfaces;
using NExpect.MatcherLogic;
using PeanutButter.Utils;
using static NExpect.Expectations;

namespace Codeo.CQRS.Tests
{
    public class SqlBuilderTests
    {
        [Test]
        public void Limit()
        {
            // Arrange
            var limit = GetRandomInt(1, 10);
            var (template, builder) = Create("select * from foo /**limit**/;");
            var expected = $"select * from foo limit {limit};";
            // Act
            builder.Limit(limit);
            var result = template.RawSql;
            // Assert
            Expect(result)
                .To.Match(expected);
        }

        private static (SqlBuilder.Template, SqlBuilder builder) Create(
            string query)
        {
            var builder = new SqlBuilder();
            return (builder.AddTemplate(query), builder);
        }
    }

    public static class StringMatchers
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
                    () => $@"Expected\n{
                            actual
                        }\nto match (normalised)\n{
                            expected
                        }\n\nnormalised values:\nactual:\n{
                            normalisedActual
                        }\nexpected:\n{
                            normalisedExpected
                        }"
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