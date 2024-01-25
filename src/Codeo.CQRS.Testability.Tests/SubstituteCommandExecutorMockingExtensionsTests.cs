using System;
using System.Linq;
using NSubstitute;
using PeanutButter.Utils;

namespace Codeo.CQRS.Testability.Tests;

[TestFixture]
public class SubstituteCommandExecutorMockingExtensionsTests
{
    [Test]
    public void ShouldBeAbleToMockBehaviorForAndInvocationOfAVoidCommand()
    {
        // Arrange
        var called = 0;
        var commandExecutor = Substitute.For<ICommandExecutor>()
            .WithMocked<Chucks>(
                _ =>
                {
                    called++;
                    throw new AccessViolationException();
                }
            );
        var id = GetRandomInt();
        var cmd = new Chucks(id);

        // Act
        Expect(() => commandExecutor.Execute(cmd))
            .To.Throw<AccessViolationException>();
        // Assert
        Expect(called)
            .To.Equal(1);
    }

    [Test]
    public void ShouldBeAbleToMockBehaviorForSpecificVoidCommand()
    {
        // Arrange
        var id = GetRandomInt(10, 100);
        var called = 0;
        var commandExecutor = Substitute.For<ICommandExecutor>()
            .WithMocked<Chucks>(
                o => o.Id == id,
                _ =>
                {
                    called++;
                    throw new FieldAccessException();
                }
            );
        var cmd = new Chucks(id);

        // Act
        Expect(() => commandExecutor.Execute(cmd))
            .To.Throw<FieldAccessException>();
        // Assert
        Expect(called)
            .To.Equal(1);
    }

    [Test]
    public void ShouldBeAbleToMockConstantValue()
    {
        // Arrange
        var expected = GetRandomInt();
        var queryExecutor = Substitute.For<ICommandExecutor>()
            .WithMocked<Add, int>(expected);
        var howMany = GetRandomInt(10, 20);
        var queries = PyLike.Range(0, howMany)
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
        var queryExecutor = Substitute.For<ICommandExecutor>()
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
        var queryExecutor = Substitute.For<ICommandExecutor>()
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

    [TestFixture]
    public class MoreFluentTesting
    {
        [Test]
        public void ShouldBeAbleToTestExecutionEasier()
        {
            // Arrange
            var id = GetRandomInt();
            var calls = 0;
            var commandExecutor = Substitute.For<ICommandExecutor>()
                .WithMocked<Chucks>(
                    q => q.Id == id,
                    _ => calls++
                );

            // Act
            commandExecutor.Execute(
                new Chucks(id)
            );

            // Assert
            commandExecutor.Received(1)
                .Execute(
                    Arg.Is<Chucks>(
                        o => o.Id == id
                    )
                );
            Expect(calls)
                .To.Equal(1);
            Expect(commandExecutor)
                .To.Have.Executed<Chucks>(
                    o => o.Id == id
                );
            Expect(commandExecutor)
                .To.Have.Executed<Chucks>(
                    o => o.Id == id,
                    "this shouldn't fail!"
                );
            Expect(commandExecutor)
                .To.Have.Executed<Chucks>(
                    o => o.Id == id,
                    () => "this too shouldn't fail!"
                );
            Expect(commandExecutor)
                .To.Have.Executed<Chucks>(
                    1,
                    o => o.Id == id
                );
            Expect(commandExecutor)
                .To.Have.Executed<Chucks>(
                    1,
                    o => o.Id == id,
                    "this should also pass"
                );
            Expect(commandExecutor)
                .To.Have.Executed<Chucks>(
                    1,
                    o => o.Id == id,
                    () => "this too should pass"
                );
        }
    }

    public class Chucks : Command
    {
        public int Id { get; }

        public Chucks(int id)
        {
            Id = id;
        }

        public override void Execute()
        {
            throw new NotImplementedException();
        }

        public override void Validate()
        {
            throw new NotImplementedException();
        }
    }

    public class Add : Command<int>
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