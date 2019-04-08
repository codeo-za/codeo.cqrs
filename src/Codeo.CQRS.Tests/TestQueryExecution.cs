using System;
using System.Collections.Generic;
using System.Transactions;
using Codeo.CQRS.Exceptions;
using NExpect;
using NUnit.Framework;
using static PeanutButter.RandomGenerators.RandomValueGen;
using PeanutButter.Utils;
using static NExpect.Expectations;

namespace Codeo.CQRS.Tests
{
    [TestFixture]
    public class TestQuery
    {
        [Test]
        public void ShouldBeAbleToReadSingleResult()
        {
            // Arrange
            var queryExecutor = new QueryExecutor();
            // Act
            var result = queryExecutor.Execute(new FindCarlSaganQuery());
            // Assert
            Expect(result).Not.To.Be.Null();
            Expect(result.Name).To.Equal("Carl Sagan");
        }

        [Test]
        public void ShouldBeAbleToInsertAndReadASingleResult()
        {
            // Arrange
            var queryExecutor = new QueryExecutor();
            var commandExecutor = new CommandExecutor();
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
            var queryExecutor = new QueryExecutor();
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
        public class WhenTransactionIsRequired: TestQuery
        {
            [Test]
            public void ShouldThrowIfNoneAvailable()
            {
                // Arrange
                var name = GetRandomString(10, 20);
                var executor = new CommandExecutor();
                // Act
                Expect(() => executor.Execute(new InsertPeople(name)))
                    .To.Throw<TransactionScopeRequired>();
                // Assert
            }

            [Test]
            public void ShouldNotThrowIfAvailable()
            {
                // Arrange
                var names = GetRandomArray<string>(5);
                var executor = new CommandExecutor();
                var result = new List<int>();
                // Act
                using (var scope = TransactionScopes.ReadCommitted(TransactionScopeOption.RequiresNew))
                {
                    Expect(() =>
                    {
                        result.AddRange(executor.Execute(new InsertPeople(names)));
                    }).Not.To.Throw();
                    
                    scope.Complete();
                }

                // Assert
                Expect(result).Not.To.Be.Empty();
                Expect(result).To.Contain.Exactly(names.Length).Items();
                var queryExecutor = new QueryExecutor();
                result.ForEach(id =>
                {
                    var inDb = queryExecutor.Execute(new FindPersonById(id));
                    Expect(names).To.Contain(inDb.Name);
                });
            }
        }

        private void CreatePerson(string name)
        {
            var commandExecutor = new CommandExecutor();
            commandExecutor.Execute(
                new CreatePerson(name)
            );
        }
    }

    public class InsertPeople : Command<IEnumerable<int>>
    {
        public IEnumerable<string> Names { get; }

        public InsertPeople(params string[] names)
        {
            Names = names;
        }

        public override void Execute()
        {
            ValidateTransactionScope();
            var ids = new List<int>();
            Names.ForEach(name =>
            {
                var id = CommandExecutor.Execute(new CreatePerson(name));
                ids.Add(id);
            });
            Result = ids;
        }
    }

    public class FindAllPeople : Query<IEnumerable<Person>>
    {
        public override void Execute()
        {
            Result = SelectMany<Person>("select * from people;");
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
            Result = SelectFirst<Person>(
                         "select * from people where id = @id;", new {Id})
                     ?? throw new PersonNotFound(Id);
        }
    }

    public class CreatePerson : Command<int>
    {
        public string Name { get; }
        public bool Enabled { get; }

        public CreatePerson(
            string name,
            bool enabled = true)
        {
            Name = name;
            Enabled = enabled;
        }

        public override void Execute()
        {
            Result = InsertGetFirst<int>(@"
insert into people (
                    name,
                    enabled,
                    created
                    ) values 
                             (
                              @name,
                              @enabled,
                              @created
                              ); 
select last_insert_id();",
                                         new
                                         {
                                             Name,
                                             Enabled,
                                             Created = DateTime.Now
                                         });
        }
    }

    public class FindPersonByName : Query<Person>
    {
        public string Name { get; }

        public FindPersonByName(string name)
        {
            Name = name;
        }

        public override void Execute()
        {
            Result = SelectFirst<Person>("select * from people where name = @name", new {Name});
        }
    }

    public class FindCarlSaganQuery : FindPersonByName
    {
        public FindCarlSaganQuery() : base("Carl Sagan")
        {
        }
    }

    public class Person : IEntity
    {
        public string Name { get; set; }
        public bool Enabled { get; set; }
        public DateTime Created { get; set; }
    }

    public class PersonNotFound : Exception
    {
        public PersonNotFound(int id) : base($"Person not found by id: {id}")
        {
        }
    }
}