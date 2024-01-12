using NSubstitute;
using static PeanutButter.Utils.PyLike;

namespace Codeo.CQRS.Testability.Tests;

[TestFixture]
public class SubstituteQueryExecutorExtensionsTests
{
    [Test]
    public void ShouldBeAbleToMockConstantValue()
    {
        // Arrange
        var expected = GetRandomInt();
        var queryExecutor = Substitute.For<IQueryExecutor>()
            .WithMocked<Add, int>(expected);
        var howMany = GetRandomInt(10, 20);
        var queries = Range(0, howMany)
            .Select(_ => new Add(GetRandomInt(), GetRandomInt()))
            .ToArray();
        // Act
        var results = queries.Select(queryExecutor.Execute);

        // Assert
        Expect(results)
            .To.Contain.All
            .Equal.To(expected);
    }

    [Test]
    public void ShouldBeAbleToMockResultForArgs()
    {
        // Arrange
        var expected = GetRandomInt();
        var a = GetRandomInt();
        var b = GetRandomInt();
        var query = new Add(a, b);
        var queryExecutor = Substitute.For<IQueryExecutor>()
            .WithMocked<Add, int>(
                q => q.A == a && q.B == b,
                expected
            );
        // Act
        var result = queryExecutor.Execute(query);
        // Assert
        Expect(result)
            .To.Equal(expected);
    }

    [Test]
    public void ShouldBeAbleToMockLogicForArgs()
    {
        // Arrange
        var a = GetRandomInt();
        var b = GetRandomInt();
        var delta = GetRandomInt(1, 10);
        var expected = a + b + delta;
        var query = new Add(a, b);
        var queryExecutor = Substitute.For<IQueryExecutor>()
            .WithMocked<Add, int>(
                q => q.A == a && q.B == b,
                q => q.A + q.B + delta
            );
        // Act
        var result = queryExecutor.Execute(query);
        // Assert
        Expect(result)
            .To.Equal(expected);
    }

    public class Add : Query<int>
    {
        public int A { get; }
        public int B { get; }

        public Add(int a, int b)
        {
            A = a;
            B = b;
        }

        public override void Execute()
        {
            Result = A + B;
        }

        public override void Validate()
        {
            // intentionally left blank
        }
    }
}