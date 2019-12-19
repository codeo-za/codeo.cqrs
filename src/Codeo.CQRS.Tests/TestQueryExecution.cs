using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Transactions;
using Codeo.CQRS.Caching;
using Codeo.CQRS.Exceptions;
using Codeo.CQRS.Tests.Commands;
using Codeo.CQRS.Tests.Models;
using Codeo.CQRS.Tests.Queries;
using NExpect;
using NUnit.Framework;
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
                    Fluently.Configure().WIthDebugMessagesDisabled();
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
            public class UpdatePersonName : UpdateCommand
            {
                public UpdatePersonName(int id, string newName)
                    : base(
                        "update people set name = @newName where id = @id;",
                        new { id, newName }
                    )
                {
                }
            }

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
            public class DeletePerson : UpdateCommand
            {
                public DeletePerson(int id)
                    : base(
                        "delete from people where id = @id;",
                        new { id }
                    )
                {
                }
            }

            public class DeletePersonWithResult : UpdateCommand<int>
            {
                public DeletePersonWithResult(int id)
                    : base(
                        "delete from people where id = @id; select max(id) from people;",
                        new { id }
                    )
                {
                }
            }

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