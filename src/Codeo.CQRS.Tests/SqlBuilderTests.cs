using NUnit.Framework;
using static PeanutButter.RandomGenerators.RandomValueGen;
using static NExpect.Expectations;

namespace Codeo.CQRS.Tests
{
    [TestFixture]
    public class SqlBuilderTests
    {
        [TestFixture]
        public class Limit
        {
            [Test]
            public void ShouldAddWhenLimitProvided()
            {
                // Arrange
                var limit = GetRandomInt();
                var (template, builder) = Create("select * from foo /**limit**/;");
                var expected = $"select * from foo limit {limit};";
                // Act
                builder.Limit(limit);
                var result = template.RawSql;
                // Assert
                Expect(result)
                    .To.Match(expected);
            }

            [Test]
            public void ShouldRemoveWhenNoLimit()
            {
                // Arrange
                var (template, builder) = Create("select * from foo /**limit**/;");
                var expected = "select * from foo;";
                // Act
                builder.Limit(GetRandomInt(1));
                builder.NoLimit();
                var result = template.RawSql;
                // Assert
                Expect(result)
                    .To.Match(expected);
            }
        }

        [TestFixture]
        public class Offset
        {
            [Test]
            public void ShouldAddOffsetWhenProvided()
            {
                // Arrange
                var offset = GetRandomInt();
                var (template, builder) = Create("select * from foo /**offset**/;");
                var expected = $"select * from foo offset {offset};";
                // Act
                builder.Offset(offset);
                var result = template.RawSql;
                // Assert
                Expect(result)
                    .To.Match(expected);
            }

            [Test]
            public void ShouldRemoveWhenNoOffset()
            {
                // Arrange
                var (template, builder) = Create("select * from foo /**offset**/;");
                var expected = $"select * from foo;";
                // Act
                builder.Offset(GetRandomInt(1));
                builder.NoOffset();
                var result = template.RawSql;
                // Assert
                Expect(result)
                    .To.Match(expected);
            }

            [Test]
            public void ShouldRemoveWhenNotConfigured()
            {
                // Arrange
                var (template, builder) = Create("select * from foo /**offset**/;");
                var expected = $"select * from foo;";
                // Act
                var result = template.RawSql;
                // Assert
                Expect(result)
                    .To.Match(expected);
            }
        }
        
        // TODO: get the rest of SqlBuilder under test
        // TODO: convert the rest of SqlBuilder to the more fluent syntax
        //       which is easier on the reader

        private static (SqlBuilder.Template, SqlBuilder builder) Create(
            string query)
        {
            var builder = new SqlBuilder();
            return (builder.AddTemplate(query), builder);
        }
    }
}