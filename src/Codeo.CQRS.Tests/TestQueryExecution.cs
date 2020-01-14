using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Transactions;
using Codeo.CQRS.Caching;
using Codeo.CQRS.Exceptions;
using Codeo.CQRS.Tests.Commands;
using Codeo.CQRS.Tests.Models;
using Codeo.CQRS.Tests.Queries;
using NExpect;
using NUnit.Framework;
using PeanutButter.Utils;
using static PeanutButter.RandomGenerators.RandomValueGen;
using static NExpect.Expectations;

namespace Codeo.CQRS.Tests
{
    [TestFixture]
    public class TestQueryExecution : TestFixtureRequiringData
    {
        [Test]
        public void ShouldBeAbleToReadSingleResult()
        {
            // Arrange
            var queryExecutor = new QueryExecutor(new NoCache());
            // Act
            var result = queryExecutor.Execute(new FindCarlSagan());
            // Assert
            Expect(result).Not.To.Be.Null();
            Expect(result.Name).To.Equal("Carl Sagan");
        }

        [Test]
        public void ShouldBeAbleToInsertAndReadASingleResult()
        {
            // Arrange
            var queryExecutor = new QueryExecutor(new NoCache());
            var commandExecutor = new CommandExecutor(queryExecutor, new NoCache());
            var name = GetRandomString(10, 20);
            var id = commandExecutor.Execute(new CreatePerson(name));
            // Act
            var result1 = queryExecutor.Execute(new FindPersonByName(name));
            var result2 = queryExecutor.Execute(new FindPersonById(id));
            // Assert
            Expect(result1).To.Intersection.Equal(new { Id = id, Name = name });
            Expect(result2).To.Intersection.Equal(new { Id = id, Name = name });
        }

        [Test]
        public void ShouldBeAbleToInsertWithNoResult()
        {
            // Arrange
            var queryExecutor = new QueryExecutor(new NoCache());
            var commandExecutor = new CommandExecutor(queryExecutor, new NoCache());
            var name = GetRandomString(10, 20);
            commandExecutor.Execute(new CreatePersonNoResult(name));
            // Act
            var result = queryExecutor.Execute(new FindPersonByName(name));
            // Assert
            Expect(result).To.Intersection.Equal(new { Name = name });
        }

        [TestFixture]
        public class SingleResultFailures : TestQueryExecution
        {
            [TestFixture]
            public class WhenDebugMessagesEnabled : SingleResultFailures
            {
                [Test]
                public void ShouldGiveDetailedMessage()
                {
                    // Arrange
                    Fluently.Configure().WithDebugMessagesEnabled();
                    var queryExecutor = new QueryExecutor(new NoCache());
                    // Act
                    Expect(() =>
                            queryExecutor.Execute(new FindPersonById(-1))
                        ).To.Throw<EntityDoesNotExistException>()
                        .With.Message.Containing(nameof(Person))
                        .And.Containing("does not exist for predicate")
                        .And.Containing("-1");
                    // Assert
                }
            }

            [TestFixture]
            public class WhenDebugMessagesDisabled : SingleResultFailures
            {
                [Test]
                public void ShouldGiveGenericMessage()
                {
                    // Arrange
                    Fluently.Configure().WithDebugMessagesDisabled();
                    var queryExecutor = new QueryExecutor(new NoCache());
                    // Act
                    Expect(() =>
                            queryExecutor.Execute(new FindPersonById(-1))
                        ).To.Throw<EntityDoesNotExistException>()
                        .With.Message.Containing(nameof(Person))
                        .And.Containing("does not exist for predicate")
                        .And.Not.Containing("-1");
                    // Assert
                }
            }
        }

        [Test]
        public void ShouldBeAbleToReadMultipleResults()
        {
            // Arrange
            var name1 = GetRandomString(10, 20);
            var name2 = GetRandomString(10, 20);
            CreatePerson(name1);
            CreatePerson(name2);
            var queryExecutor = new QueryExecutor(new NoCache());
            // Act
            var results = queryExecutor.Execute(
                new FindAllPeople()
            );
            // Assert
            Expect(results).Not.To.Be.Empty();
            Expect(results).To.Contain.Exactly(1).Matched.By(p => p.Name == name1);
            Expect(results).To.Contain.Exactly(1).Matched.By(p => p.Name == name2);
        }

        [TestFixture]
        public class WhenTransactionIsRequired : TestQueryExecution
        {
            [Test]
            public void ShouldThrowIfNoneAvailable()
            {
                // Arrange
                var name = GetRandomString(10, 20);
                var cache = new NoCache();
                var executor = new CommandExecutor(
                    new QueryExecutor(cache),
                    cache
                );
                // Act
                Expect(() => executor.Execute(new CreatePeople(name)))
                    .To.Throw<TransactionScopeRequired>();
                // Assert
            }

            [Test]
            [Explicit("Runs fine by itself, but in the full test pack, something is making TimeSpan.Zero != 0")]
            public void ShouldNotThrowIfAvailable()
            {
                // Arrange
                var names = GetRandomArray<string>(5);
                var cache = new NoCache();
                var executor = new CommandExecutor(
                    new QueryExecutor(cache),
                    cache
                );
                var result = new List<int>();

                Expect(TimeSpan.Zero.Ticks)
                    .To.Equal(0, () => $"WTF: expected TimeSpan.Zero to be zero, but it's {TimeSpan.Zero}");
                // Act
                using (var scope =
                    TransactionScopes.ReadCommitted(TransactionScopeOption.RequiresNew
                    )
                )
                {
                    Expect(() =>
                    {
                        result.AddRange(executor.Execute(new CreatePeople(names)));
                    }).Not.To.Throw();

                    scope.Complete();
                }

                // Assert
                Expect(result).Not.To.Be.Empty();
                Expect(result).To.Contain.Exactly(names.Length).Items();
                var queryExecutor = new QueryExecutor(cache);
                result.ForEach(id =>
                {
                    var inDb = queryExecutor.Execute(new FindPersonById(id));
                    Expect(names).To.Contain(inDb.Name);
                });
            }
        }

        [Test]
        public void ShouldBeAbleToReadSingleResultOfNonEntity()
        {
            // Arrange
            var queryExecutor = new QueryExecutor(new NoCache());
            // Act
            var result = queryExecutor.Execute(new FindCarlSaganAlike());
            // Assert
            Expect(result).Not.To.Be.Null();
            Expect(result.Name).To.Equal("Carl Sagan");
            Expect(result.DateOfBirth).To.Equal(new DateTime(1934, 11, 9, 0, 0, 0, DateTimeKind.Utc));
        }

        [Test]
        public void ShouldBeAbleToReadManyResultsOfNonEntity()
        {
            // Arrange
            var queryExecutor = new QueryExecutor(new NoCache());
            var query = new FindCarlSaganAlikes();
            // Act
            var results = queryExecutor.Execute(query);
            // Assert
            Expect(results).Not.To.Be.Null();
            Expect(results).To.Contain.Exactly(1).Item();
            var result = results.First();
            Expect(result.Name).To.Equal("Carl Sagan");
            Expect(result.DateOfBirth).To.Equal(new DateTime(1934, 11, 9, 0, 0, 0, DateTimeKind.Utc));
        }

        [TestFixture]
        public class TestGenericUpdateCommand : TestQueryExecution
        {
            [Test]
            public void ShouldUpdate()
            {
                // Arrange
                var oldName = GetRandomString(10, 20);
                var id = CreatePerson(oldName);
                var newName = GetAnother(oldName);
                var sut = new UpdatePersonName(id, newName);
                // Act
                CommandExecutor.Execute(sut);
                // Assert
                var inDb = FindPersonById(id);
                Expect(inDb.Name).To.Equal(newName);
            }
        }

        [TestFixture]
        public class TestGenericDeleteCommand : TestQueryExecution
        {
            [Test]
            public void ShouldDelete()
            {
                // Arrange
                var oldName = GetRandomString(10, 20);
                var id = CreatePerson(oldName);
                var sut = new DeletePerson(id);
                // Act
                CommandExecutor.Execute(sut);
                // Assert
                var inDb = FindPersonById(id);
                Expect(inDb).To.Be.Null();
            }

            [Test]
            public void ShouldDeleteAndReturnValue()
            {
                // Arrange
                var oldName = GetRandomString(10, 20);
                var id = CreatePerson(oldName);
                var sut = new DeletePersonWithResult(id);
                // Act
                var result = CommandExecutor.Execute(sut);
                // Assert
                var inDb = FindPersonById(result);
                Expect(inDb).Not.To.Be.Null();
            }
        }

        [TestFixture]
        public class TestCachingByAttribute : TestQueryExecution
        {
            [TestFixture]
            public class WhenNotDecorated : TestCachingByAttribute
            {
                [Test]
                public void ShouldNotCache()
                {
                    // Arrange
                    var name1 = GetRandomString();
                    var name2 = GetAnother(name1);
                    var name3 = GetAnother<string>(new[] { name1, name2 });
                    var id = CreatePerson(name1);
                    // Act
                    var first = QueryExecutor.Execute(
                        new FindPersonByIdUncached(
                            id
                        )
                    );
                    UpdatePersonName(id, name2);
                    var second = QueryExecutor.Execute(
                        new FindPersonByIdUncached(
                            id
                        )
                    );
                    UpdatePersonName(id, name3);
                    var third = QueryExecutor.Execute(
                        new FindPersonByIdUncached(
                            id
                        )
                    );
                    // Assert
                    Expect(first.Name)
                        .To.Equal(name1);
                    Expect(second.Name)
                        .To.Equal(name2);
                    Expect(third.Name)
                        .To.Equal(name3);
                }
            }

            [TestFixture]
            public class OnSelectQueries : TestCachingByAttribute
            {
                [Test]
                public void ShouldUseCache()
                {
                    using (new AutoResetter(
                        UseMemoryCache,
                        UseNoCache))
                    {
                        // Arrange
                        var expected = GetRandomString(10, 20);
                        var unexpected = GetAnother(expected);
                        var id = CreatePerson(expected);
                        var query = new FindPersonById(id);
                        // Act
                        var inDb = QueryExecutor.Execute(query);
                        CommandExecutor.Execute(new UpdatePersonName(id, unexpected));
                        var shouldBeCached = QueryExecutor.Execute(query);
                        // Assert
                        Expect(inDb.Name)
                            .To.Equal(expected);
                        Expect(shouldBeCached.Name)
                            .To.Equal(expected, () => $"Should get cached original name: {expected}");
                    }
                }

                [Test]
                public void ShouldNotUseCacheWhenCacheKeyPropertiesDiffer()
                {
                    // Arrange
                    var name1 = GetRandomString(10, 20);
                    var name2 = GetAnother(name1);
                    var id1 = CreatePerson(name1);
                    var id2 = CreatePerson(name2);
                    var query1 = new FindPersonById(id1);
                    var query2 = new FindPersonById(id2);
                    // Act
                    var person1 = QueryExecutor.Execute(query1);
                    var person2 = QueryExecutor.Execute(query2);
                    // Assert
                    Expect(person1.Name)
                        .To.Equal(name1);
                    Expect(person2.Name)
                        .To.Equal(name2);
                }

                [Test]
                public void ShouldAutomaticallyExpire()
                {
                    using (new AutoResetter(
                        UseMemoryCache,
                        UseNoCache))
                    {
                        // Arrange
                        var originalName = GetRandomString(10, 20);
                        var newName = GetAnother(originalName);
                        var id = CreatePerson(originalName);
                        var query = new FindPersonByIdShortLived(id);
                        // Act
                        var inDb = QueryExecutor.Execute(query);
                        CommandExecutor.Execute(new UpdatePersonName(id, newName));
                        var shouldBeCached = QueryExecutor.Execute(query);
                        Thread.Sleep(1500);
                        var shouldNotBeCached = QueryExecutor.Execute(query);
                        // Assert
                        Expect(inDb.Name)
                            .To.Equal(originalName);
                        Expect(shouldBeCached.Name)
                            .To.Equal(originalName, () => $"Should get cached original name: {originalName}");
                        Expect(shouldNotBeCached.Name)
                            .To.Equal(newName,
                                () => $"Should have expired the cached item and retrieved new name: {newName}");
                    }
                }
            }

            [TestFixture]
            public class ShouldNeverUseOnTransformQueries : TestCachingByAttribute
            {
                [TestFixture]
                public class WhenIsDelete : ShouldNeverUseOnTransformQueries
                {
                    [Test]
                    public void ShouldNotUse()
                    {
                        // Arrange
                        var name = GetRandomString(10);
                        var other = GetAnother(name);
                        var updated = GetAnother<string>(new[] { name, other });
                        var updated2 = GetAnother<string>(new[] { name, other, updated });
                        var idToDelete = CreatePerson(name);
                        var idToUpdate = CreatePerson(other);

                        // Act
                        CommandExecutor.Execute(
                            new DeletePersonNoResultWithSideEffects(idToDelete, idToUpdate, updated)
                        );
                        var initialResult = FindPersonById(idToUpdate);
                        Expect(initialResult.Name)
                            .To.Equal(updated);
                        CommandExecutor.Execute(
                            new DeletePersonNoResultWithSideEffects(idToDelete, idToUpdate, updated2)
                        );
                        var secondResult = FindPersonById(idToUpdate);

                        // Assert
                        Expect(secondResult.Name)
                            .To.Equal(updated2);
                    }

                    [Test]
                    public void ShouldNotUseWithResults()
                    {
                        // Arrange
                        var name = GetRandomString(10);
                        var other = GetAnother(name);
                        var updated = GetAnother<string>(new[] { name, other });
                        var idToDelete = CreatePerson(name);
                        var idToUpdate = CreatePerson(other);

                        // Act
                        var initialResult = CommandExecutor.Execute(
                            new DeletePersonWithArbResult(idToDelete, idToUpdate)
                        );
                        Expect(initialResult.Name)
                            .To.Equal(other);
                        UpdatePersonName(idToUpdate, updated);
                        var secondResult = CommandExecutor.Execute(
                            new DeletePersonWithArbResult(idToDelete, idToUpdate)
                        );

                        // Assert
                        Expect(secondResult.Name)
                            .To.Equal(updated);
                    }
                }

                [TestFixture]
                public class WhenIsInsert : ShouldNeverUseOnTransformQueries
                {
                    [Test]
                    public void ShouldNotUse()
                    {
                        // Arrange
                        var originalName = GetRandomString(10);
                        var update1 = GetAnother(originalName);
                        var update2 = GetAnother<string>(new[] { originalName, update1 });
                        var id = CreatePerson(originalName);
                        // Act
                        CommandExecutor.Execute(
                            new CreatePersonWithSideEffect(
                                GetRandomString(10),
                                id,
                                update1)
                        );

                        Expect(NameOfPerson(id))
                            .To.Equal(update1);

                        CommandExecutor.Execute(
                            new CreatePersonWithSideEffect(
                                GetRandomString(10),
                                id,
                                update2
                            ));
                        // Assert
                        Expect(NameOfPerson(id))
                            .To.Equal(update2);
                    }

                    [Test]
                    public void ShouldNotUseOnWithResult()
                    {
                        // Arrange
                        var name = GetRandomString(10);
                        var cmd = new CreatePerson(name);
                        // Act
                        var result1 = CommandExecutor.Execute(cmd);
                        var result2 = CommandExecutor.Execute(cmd);
                        // Assert
                        Expect(result1).Not.To.Equal(result2);
                        Expect(FindPersonById(result1).Name)
                            .To.Equal(name);
                        Expect(FindPersonById(result2).Name)
                            .To.Equal(name);
                    }
                }

                [TestFixture]
                public class WhenIsUpdate : ShouldNeverUseOnTransformQueries
                {
                    [Test]
                    public void ShouldNotUse()
                    {
                        // Arrange
                        var name1 = GetRandomString();
                        var name2 = GetAnother(name1);
                        var id = CreatePerson(GetRandomString());
                        // Act
                        CommandExecutor.Execute(
                            new UpdatePersonName(
                                id,
                                name1
                            )
                        );
                        Expect(NameOfPerson(id))
                            .To.Equal(name1);
                        CommandExecutor.Execute(
                            new UpdatePersonName(
                                id,
                                name2
                            )
                        );
                        // Assert
                        Expect(NameOfPerson(id))
                            .To.Equal(name2);
                    }

                    [Test]
                    public void ShouldNotUseOnResults()
                    {
                        // Arrange
                        var name1 = GetRandomString();
                        var name2 = GetAnother(name1);
                        var id = CreatePerson(GetRandomString());
                        // Act
                        var result1 = CommandExecutor.Execute(
                            new UpdatePersonNameWithResult(
                                id,
                                name1
                            )
                        );
                        Expect(NameOfPerson(id))
                            .To.Equal(name1);
                        Expect(result1)
                            .To.Equal(name1);

                        var result2 = CommandExecutor.Execute(
                            new UpdatePersonNameWithResult(
                                id,
                                name2
                            )
                        );
                        // Assert
                        Expect(NameOfPerson(id))
                            .To.Equal(name2);
                        Expect(result2)
                            .To.Equal(name2);
                    }
                }
            }

            [TestFixture]
            public class WhenSpecifiedCacheKeyPropsNotFound: TestQueryExecution
            {
                [Test]
                public void ShouldThrow()
                {
                    // Arrange
                    var id = CreatePerson(GetRandomString());
                    var sut = new FindPersonByIdWithInvalidCacheProp(id);
                    // Act
                    Expect(() => QueryExecutor.Execute(sut))
                        .To.Throw<InvalidCachePropertiesSpecified>()
                        .With.Message.Containing("BadProp");
                    // Assert
                }
            }

            [TestFixture]
            public class WhenSpecifiedCacheKeyPropsArePrivate: TestQueryExecution
            {
                [Test]
                public void ShouldCacheByThatKey()
                {
                    // Arrange
                    using (new AutoResetter(UseMemoryCache, UseNoCache))
                    {
                        var name = "original"; // GetRandomString(10);
                        var updated = "updated"; // GetAnother(name);
                        var another = "another person"; //GetAnother<string>(new[] { name, updated });
                        var id = CreatePerson(name);
                        var otherId = CreatePerson(another);
                        var query = new FindPersonByIdWithPrivateCacheProp(id);
                        // Act
                        var result1 = QueryExecutor.Execute(query);
                        UpdatePersonName(id, updated);
                        var result2 = QueryExecutor.Execute(query);
                        var result3 = QueryExecutor.Execute(
                            new FindPersonByIdWithPrivateCacheProp(otherId));
                        // Assert
                        // should be updated in the database
                        Expect(FindPersonById(id).Name)
                            .To.Equal(updated);
                        // should have the original value
                        Expect(result1.Name)
                            .To.Equal(name);
                        // should have the cached value
                        Expect(result2.Name)
                            .To.Equal(name);
                        // should not get the cached value for a different id
                        Expect(result3.Name)
                            .To.Equal(another);
                    }
                }
            }
            
            private string NameOfPerson(int id)
            {
                return QueryExecutor.Execute(
                    new FindPersonById(id)
                    {
                        ShouldInvalidateCache = true
                    }
                ).Name;
            }

            private static void UpdatePersonName(int id, string newName)
            {
                CommandExecutor.Execute(
                    new UpdatePersonName(
                        id,
                        newName
                    )
                );
            }

            private static void UseNoCache()
            {
                var cache = new NoCache();
                QueryExecutor = new QueryExecutor(cache);
                CommandExecutor = new CommandExecutor(QueryExecutor, cache);
            }

            private static void UseMemoryCache()
            {
                var cache = new MemoryCache();
                QueryExecutor = new QueryExecutor(cache);
                CommandExecutor = new CommandExecutor(QueryExecutor, cache);
            }
        }

        private static ICache NoCache = new NoCache();
        private static IQueryExecutor QueryExecutor = new QueryExecutor(NoCache);
        private static ICommandExecutor CommandExecutor = new CommandExecutor(QueryExecutor, NoCache);

        private Person FindPersonById(int id)
        {
            try
            {
                return QueryExecutor.Execute(
                    new FindPersonById(id)
                );
            }
            catch (EntityDoesNotExistException)
            {
                return null;
            }
        }

        private int CreatePerson(string name)
        {
            var cache = new NoCache();
            var executor = new CommandExecutor(
                new QueryExecutor(cache),
                cache
            );
            return executor.Execute(
                new CreatePerson(name)
            );
        }
    }
}