using System;
using System.Collections.Generic;
using System.Linq;
using NSubstitute;
using PeanutButter.Utils;
using static PeanutButter.Utils.PyLike;

namespace Codeo.CQRS.Testability.Tests;

[TestFixture]
public class SubstituteQueryExecutorMockingExtensionsTests
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

    [Test]
    public void ShouldBeAbleToMockBodyOnly()
    {
        // Arrange
        var store = new Dictionary<int, Person>()
        {
            [1] = GetRandom<Person>().With(o => o.Id = 1),
            [2] = GetRandom<Person>().With(o => o.Id = 2),
            [3] = GetRandom<Person>().With(o => o.Id = 3)
        };
        var queryExecutor = Substitute.For<IQueryExecutor>()
            .WithMocked<FindPersonById, Person>(
                q => store.GetValueOrDefault(q.Id)
            );
        var id1 = GetRandomInt(1, 3);
        var missingId = GetRandomInt(10);
        // Act
        var result1 = queryExecutor.Execute(
            new FindPersonById(id1)
        );
        var result2 = queryExecutor.Execute(
            new FindPersonById(missingId)
        );
        // Assert
        Expect(result1)
            .To.Be(store[id1]);
        Expect(result2)
            .To.Be.Null();
    }

    [TestFixture]
    public class MoreFluentTesting
    {
        [Test]
        public void ShouldBeAbleToTestExecutionEasier()
        {
            // Arrange
            var person = GetRandom<Person>();
            var queryExecutor = Substitute.For<IQueryExecutor>()
                .WithMocked<FindPersonById, Person>(
                    q => q.Id == person.Id,
                    _ => person
                );

            // Act
            var result = queryExecutor.Execute(
                new FindPersonById(person.Id)
            );

            // Assert
            queryExecutor.Received(1)
                .Execute(
                    Arg.Is<FindPersonById>(
                        o => o.Id == person.Id
                    )
                );
            Expect(result)
                .To.Be(person);
            Expect(queryExecutor)
                .To.Have.Executed<FindPersonById>(
                    o => o.Id == person.Id,
                    "this shouldn't fail!"
                );
            Expect(queryExecutor)
                .To.Have.Executed<FindPersonById>(
                    o => o.Id == person.Id,
                    () => "this too shouldn't fail!"
                );
            Expect(queryExecutor)
                .To.Have.Executed<FindPersonById>(
                    1,
                    o => o.Id == person.Id
                );
            Expect(queryExecutor)
                .To.Have.Executed<FindPersonById>(
                    1,
                    o => o.Id == person.Id,
                    "this should also pass"
                );
            Expect(queryExecutor)
                .To.Have.Executed<FindPersonById>(
                    1,
                    o => o.Id == person.Id,
                    () => "this too should pass"
                );
        }
    }

    public class FindPersonById : Query<Person>
    {
        public int Id { get; }

        public FindPersonById(int id)
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

    public class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
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