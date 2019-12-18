using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Codeo.CQRS.Caching;
using Codeo.CQRS.Exceptions;
using Codeo.CQRS.Tests.Commands;
using Codeo.CQRS.Tests.Queries;
using NExpect;
using NUnit.Framework;
using static PeanutButter.RandomGenerators.RandomValueGen;
using static NExpect.Expectations;

namespace Codeo.CQRS.Tests
{
    [TestFixture]
    public class TestQueryExecution: TestFixtureRequiringData
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
            commandExecutor.Execute(new CreatePerson(name));
            // Act
            var result = queryExecutor.Execute(new FindPersonByName(name));
            // Assert
            Expect(result).To.Intersection.Equal(new {Name = name});
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
        public class WhenTransactionIsRequired: TestQueryExecution
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
                // Act
                using (var scope = TransactionScopes.ReadCommitted(TransactionScopeOption.RequiresNew))
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


        private void CreatePerson(string name)
        {
            var cache = new NoCache();
            var executor = new CommandExecutor(
                new QueryExecutor(cache),
                cache
            );
            executor.Execute(
                new CreatePerson(name)
            );
        }
    }
}