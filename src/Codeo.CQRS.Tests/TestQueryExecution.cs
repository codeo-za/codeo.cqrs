using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Transactions;
using Codeo.CQRS.Caching;
using Codeo.CQRS.Exceptions;
using Codeo.CQRS.Tests.Commands;
using Codeo.CQRS.Tests.Models;
using Codeo.CQRS.Tests.Queries;
using NExpect;
using NExpect.Implementations;
using NExpect.Interfaces;
using NExpect.MatcherLogic;
using NUnit.Framework;
using PeanutButter.Utils;
using static PeanutButter.RandomGenerators.RandomValueGen;
using static NExpect.Expectations;
using MemoryCache = Codeo.CQRS.Caching.MemoryCache;

namespace Codeo.CQRS.Tests
{
    [TestFixture]
    public class TestQueryExecution : TestFixtureRequiringData
    {
        [TestFixture]
        public class ConvenienceSelectMethods : TestFixtureRequiringData
        {
            [Test]
            public void ShouldBeAbleToSelectAList()
            {
                // Arrange
                var id = CreatePerson(GetRandomName());

                // Act
                var result = QueryExecutor.Execute(new AllPeopleInAList());
                // Assert
                Expect(result)
                    .Not.To.Be.Empty();
                Expect(result)
                    .To.Be.An.Instance.Of<List<Person>>();
                Expect(result)
                    .To.Contain.Exactly(1)
                    .Matched.By(p => p.Id == id);
            }

            [Test]
            public void ShouldBeAbleToSelectAnArray()
            {
                // Arrange
                var id = CreatePerson(GetRandomName());

                // Act
                var result = QueryExecutor.Execute(new AllPeopleInAnArray());
                // Assert
                Expect(result)
                    .Not.To.Be.Empty();
                Expect(result)
                    .To.Be.An.Instance.Of<Person[]>();
                Expect(result)
                    .To.Contain.Exactly(1)
                    .Matched.By(p => p.Id == id);
            }

            [Test]
            public void ShouldBeAbleToSelectFirstOrDefault()
            {
                // Arrange
                // Act
                var result1 = QueryExecutor.Execute(
                    new PerhapsFindPersonById(-1)
                );
                var id = CreatePerson(GetRandomName());
                var result2 = QueryExecutor.Execute(
                    new PerhapsFindPersonById(id)
                );
                // Assert

                Expect(result1)
                    .To.Be.Null();
                Expect(result2)
                    .To.Be.Matched.By(o => o.Id == id);
            }

            public class PerhapsFindPersonById : Query<Person>
            {
                public int Id { get; }

                public PerhapsFindPersonById(int id)
                {
                    Id = id;
                }

                public override void Execute()
                {
                    Result = SelectFirstOrDefault<Person>(
                        "select * from people where id = @id",
                        new { id = Id }
                    );
                }

                public override void Validate()
                {
                }
            }

            public class AllPeopleInAnArray : Query<Person[]>
            {
                public override void Execute()
                {
                    Result = SelectArray<Person>("select * from people");
                }

                public override void Validate()
                {
                }
            }

            public class AllPeopleInAList : Query<List<Person>>
            {
                public override void Execute()
                {
                    Result = SelectList<Person>("select * from people");
                }

                public override void Validate()
                {
                }
            }
        }

        [TestFixture]
        public class WhenQueryingWithSingleDataSetResults : TestFixtureRequiringData
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
            public void ShouldAutoRegisterTypeMappingsAsRequired()
            {
                // Arrange
                var queryExecutor = new QueryExecutor(new NoCache());
                var query = new FindCarlSaganAlikes();
                // Act
                var results = queryExecutor.Execute(query);
                // Assert
                Expect(results).Not.To.Be.Null();
                Expect(results)
                    .To.Contain.Exactly(1).Item();
                var result = results.First();
                Expect(result.Name)
                    .To.Equal("Carl Sagan");
                Expect(result.DateOfBirth)
                    .To.Equal(new DateTime(1934, 11, 9, 0, 0, 0, DateTimeKind.Utc));
            }
        }

        [TestFixture]
        public class WhenQueryingWithJoinsRequiringPerRowMapping : TestFixtureRequiringData
        {
            [Test]
            public void ShouldBeAbleToQueryAcrossTwoIncludedTables()
            {
                // Arrange
                var departmentName1 = GetRandomString(5);
                var departmentId1 = CreateDepartment(departmentName1);
                var departmentName2 = GetRandomString(5);
                var departmentId2 = CreateDepartment(departmentName2);
                var personName = GetRandomString(5);
                var personId = CreatePerson(personName);
                AssociatePersonWithDepartment(personId, departmentId1);
                AssociatePersonWithDepartment(personId, departmentId2);
                // Act
                var result = QueryExecutor.Execute(
                    new FindPersonWithDepartments(
                        personId
                    )
                );
                // Assert
                Expect(result)
                    .Not.To.Be.Null();
                var person = FindPersonById(personId);
                Expect(result)
                    .To.Intersection.Equal(person);
                var departments = FindDepartmentsById(departmentId1, departmentId2);
                Expect(result.Departments)
                    .To.Be.Deep.Equivalent.To(departments);
            }

            [Test]
            public void ShouldBeAbleToQueryAcrossThreeIncludedTables()
            {
                // Arrange
                var departmentName1 = GetRandomString(5);
                var departmentId1 = CreateDepartment(departmentName1);
                var departmentName2 = GetRandomString(5);
                var departmentId2 = CreateDepartment(departmentName2);
                var personName = GetRandomString(5);
                var personId = CreatePerson(personName);
                AssociatePersonWithDepartment(personId, departmentId1);
                AssociatePersonWithDepartment(personId, departmentId2);
                var dept1Tags = FindTagsById(new[]
                {
                    CreateTagForDepartment(departmentId1, GetRandomString(5)),
                    CreateTagForDepartment(departmentId1, GetRandomString(5)),
                });
                var dept2Tags = FindTagsById(new[]
                {
                    CreateTagForDepartment(departmentId2, GetRandomString(5)),
                    CreateTagForDepartment(departmentId2, GetRandomString(5)),
                });
                // Act
                var result = QueryExecutor.Execute(
                    new FindPersonWithDepartmentsAndTags(
                        personId
                    )
                );
                // Assert
                Expect(result)
                    .Not.To.Be.Null();
                var person = FindPersonById(personId);
                Expect(result)
                    .To.Intersection.Equal(person);
                var departments = FindDepartmentsById(
                    departmentId1,
                    departmentId2
                );
                Expect(result.Departments.Select(d => d as Department))
                    .To.Be.Intersection.Equivalent.To(departments);
                var dept1 = result.Departments.Single(d => d.Id == departmentId1);
                Expect(dept1.Tags)
                    .To.Be.Intersection.Equivalent.To(dept1Tags);
                var dept2 = result.Departments.Single(d => d.Id == departmentId2);
                Expect(dept2.Tags)
                    .To.Be.Intersection.Equivalent.To(dept2Tags);
            }

            private DepartmentTag[] FindTagsById(int[] ids)
            {
                return QueryExecutor.Execute(
                    new FindTagsById(
                        ids
                    )
                ).ToArray();
            }

            private int CreateTagForDepartment(
                int departmentId,
                string tag)
            {
                return CommandExecutor.Execute(
                    new CreateTagForDepartment(
                        departmentId,
                        tag
                    )
                );
            }
        }

        [TestFixture]
        public class WhenQueryingWithMultipleResultSets : TestFixtureRequiringData
        {
            [Test]
            public void ShouldBeAbleToUseAllResultSets()
            {
                // Arrange
                var personName = GetRandomString(5);
                var personId = CreatePerson(personName);
                var departmentName = GetRandomString(5);
                var departmentId = CreateDepartment(departmentName);
                // Act
                var result = QueryExecutor.Execute(
                    new FindAllPeopleAndDepartments()
                );
                // Assert
                // result likely has more than just the entities
                //    created in this test -- which is OK
                Expect(result.People)
                    .Not.To.Be.Empty();
                var person = FindPersonById(personId);
                Expect(result.People)
                    .To.Contain.Exactly(1)
                    .Deep.Equal.To(person);
                Expect(result.Departments)
                    .Not.To.Be.Empty();
                var department = FindDepartmentsById(departmentId).Single();
                Expect(result.Departments)
                    .To.Contain.Exactly(1)
                    .Deep.Equal.To(department);
            }
        }

        [TestFixture]
        public class SingleResultFailures : TestFixtureRequiringData
        {
            [TestFixture]
            public class WhenDebugMessagesEnabled : TestFixtureRequiringData
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
                        ).To.Throw<EntityNotFoundException>()
                        .With.Message.Containing(nameof(Person))
                        .And.Containing("does not exist for predicate")
                        .And.Containing("-1");
                    // Assert
                }
            }

            [TestFixture]
            public class WhenDebugMessagesDisabled : TestFixtureRequiringData
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
                        ).To.Throw<EntityNotFoundException>()
                        .With.Message.Containing(nameof(Person))
                        .And.Containing("does not exist for predicate")
                        .And.Not.Containing("-1");
                    // Assert
                }
            }
        }

        [TestFixture]
        public class WhenTransactionIsRequired : TestFixtureRequiringData
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

        [TestFixture]
        public class ProvidingMultipleFilters : TestFixtureRequiringData
        {
            [Test]
            public void ShouldBeAbleToProvideMultipleFilters()
            {
                // Arrange
                var name = GetRandomString(10, 20);
                var id = CreatePerson(name);
                var qry = new FindPersonByNameAndId(name, id);
                // Act
                var result = QueryExecutor.Execute(qry);
                // Assert
                Expect(result)
                    .Not.To.Be.Null();
                Expect(result.Id)
                    .To.Equal(id);
                Expect(result.Name)
                    .To.Equal(name);
            }

            public class FindPersonByNameAndId : SelectQuery<Person>
            {
                public FindPersonByNameAndId(
                    string name,
                    int id
                ) : base(
                    "select * from people /**where**/",
                    ("id = @id", new { id }),
                    ("name = @name", new { name })
                )
                {
                }

                public override void Validate()
                {
                }
            }
        }

        [TestFixture]
        public class WhenUpdating : TestFixtureRequiringData
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
        public class WhenDeleting : TestFixtureRequiringData
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
        public class WhenDirectingCachingViaAttribute : TestFixtureRequiringData
        {
            [TestFixture]
            public class WhenNotDecorated : TestFixtureRequiringData
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
            public class CachingQueries
            {
                // CachingQuery is a shorthand: reduces caching options
                // to either all-in or write-only, and is how many consumers
                // will expect to work with caching

                [TestFixture]
                public class WhenUseCacheIsTrue : TestFixtureRequiringData
                {
                    [Test]
                    public void ShouldReadFromCache()
                    {
                        // Arrange
                        using var _ = UseMemoryCacheOnce();
                        Expect(QueryExecutor)
                            .To.Have.MemoryCache();
                        var name1 = GetRandomName();
                        var id = CreatePerson(name1);
                        var sut = new FindPersonByIdCaching(id, true);
                        Expect(sut.UseCache)
                            .To.Be.True();
                        var name2 = GetAnother(name1, GetRandomName);
                        // Act
                        var first = QueryExecutor.Execute(
                            new FindPersonByIdCaching(
                                id,
                                true
                            )
                        );
                        UpdatePersonName(id, name2);
                        var second = QueryExecutor.Execute(
                            new FindPersonByIdCaching(
                                id,
                                true
                            )
                        );
                        // Assert
                        Expect(first.Name)
                            .To.Equal(name1, () => "Should retrieve the original name");
                        Expect(second.Name)
                            .To.Equal(name1, () => "Should not see updated name");
                    }
                }

                [TestFixture]
                public class WhenUseCacheIsFalse : TestFixtureRequiringData
                {
                    [Test]
                    public void ShouldOnlyWriteToCache()
                    {
                        // Arrange
                        var memCache = new MemoryCache();
                        memCache.Clear();
                        Expect(memCache.Count)
                            .To.Equal(
                                0, () => $"Cached keys: {memCache.Keys.JoinWith(",")}");
                        using var _ = UseCacheOnce(memCache);
                        Expect(QueryExecutor)
                            .To.Have.MemoryCache();
                        var name1 = GetRandomName();
                        var id = CreatePerson(name1);
                        var sut = new FindPersonByIdCaching(id, true);
                        Expect(sut.UseCache)
                            .To.Be.True();
                        var name2 = GetAnother(name1, GetRandomName);
                        var cacheKey = sut.CacheKey;
                        // Act
                        
                        var first = QueryExecutor.Execute(
                            new FindPersonByIdCaching(
                                id,
                                false
                            )
                        );
                        Expect(memCache.Count)
                            .To.Equal(1);
                        Expect(memCache.ContainsKey(cacheKey))
                            .To.Be.True();
                        UpdatePersonName(id, name2);
                        var second = QueryExecutor.Execute(
                            new FindPersonByIdCaching(
                                id,
                                false
                            )
                        );
                        Expect(memCache.Count)
                            .To.Equal(1);
                        Expect(memCache.ContainsKey(cacheKey))
                            .To.Be.True();
                        // Assert
                        Expect(first.Name)
                            .To.Equal(name1, () => "Should retrieve the original name");
                        Expect(second.Name)
                            .To.Equal(name2, () => "Should read updated name");
                        var cached = memCache.Get<Person>(cacheKey);
                        Expect(cached.Name)
                            .To.Equal(name2, () => "Latest query result should be cached");
                    }
                }
            }

            [TestFixture]
            public class OnSelectQueries : TestFixtureRequiringData
            {
                [TestFixture]
                public class WhenQueryIsDecoratedWithCacheAttribute
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

                    [Test]
                    public void CollectionPropertiesInCacheKeysShouldIncludeAllValues()
                    {
                        // Arrange
                        var ids = GetRandomCollection<int>(2, 4);
                        var qry = new FindPeopleByIds(ids);
                        var expected = @$"{
                            nameof(FindPeopleByIds)
                        }-Ids::{
                            string.Join(",", qry.Ids)
                        }";
                        // Act
                        var result = qry.GenerateCacheKeyForTesting();
                        // Assert
                        Expect(result)
                            .To.Equal(expected);
                    }
                }
            }

            [TestFixture]
            public class ShouldNeverUseOnTransformQueries : TestFixtureRequiringData
            {
                [TestFixture]
                public class WhenIsDelete : TestFixtureRequiringData
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
                public class WhenIsInsert : TestFixtureRequiringData
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
                public class WhenIsUpdate : TestFixtureRequiringData
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
            public class WhenSpecifiedCacheKeyPropsNotFound : TestFixtureRequiringData
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
            public class WhenSpecifiedCacheKeyPropsArePrivate : TestFixtureRequiringData
            {
                [Test]
                public void ShouldCacheByThatKey()
                {
                    // Arrange
                    using var _ = UseMemoryCacheOnce();
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

            private static string NameOfPerson(int id)
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
                QueryExecutor = new QueryExecutor(() => cache);
                CommandExecutor = new CommandExecutor(QueryExecutor, cache);
            }

            private static void UseMemoryCache()
            {
                var cache = new MemoryCache();
                cache.Clear();
                QueryExecutor = new QueryExecutor(cache);
                CommandExecutor = new CommandExecutor(QueryExecutor, cache);
            }

            private static void UseCache(ICache cache)
            {
                QueryExecutor = new QueryExecutor(cache);
                CommandExecutor = new CommandExecutor(QueryExecutor, cache);
            }

            private static IDisposable UseCacheOnce(ICache cache)
            {
                return AutoResetter.Create(
                    () => UseCache(cache),
                    UseNoCache
                );
            }

            private static IDisposable UseMemoryCacheOnce()
            {
                return AutoResetter.Create(
                    UseMemoryCache,
                    UseNoCache
                );
            }
        }

        private static readonly ICache NoCache = new NoCache();
        private static IQueryExecutor QueryExecutor = new QueryExecutor(NoCache);
        private static ICommandExecutor CommandExecutor = new CommandExecutor(QueryExecutor, NoCache);

        private static Person FindPersonById(int id)
        {
            try
            {
                return QueryExecutor.Execute(
                    new FindPersonById(id)
                );
            }
            catch (EntityNotFoundException)
            {
                return null;
            }
        }

        private static int CreateDepartment(string name)
        {
            return CommandExecutor.Execute(
                new CreateDepartment(name)
            );
        }

        private static int CreatePerson(string name)
        {
            return CommandExecutor.Execute(
                new CreatePerson(name)
            );
        }

        private static void AssociatePersonWithDepartment(
            int personId,
            int departmentId
        )
        {
            CommandExecutor.Execute(
                new AddPersonToDepartment(
                    personId,
                    departmentId
                )
            );
        }

        private static IEnumerable<Department> FindDepartmentsById(
            params int[] ids)
        {
            return QueryExecutor.Execute(
                new FindDepartmentsById(
                    ids
                )
            );
        }
    }

    public static class QueryExecutorExtensions
    {
        public static IMore<IQueryExecutor> MemoryCache(
            this IHave<IQueryExecutor> have
        )
        {
            return have.AddMatcher(actual =>
            {
                if (actual is not QueryExecutor qe)
                {
                    return new EnforcedMatcherResult(
                        false,
                        () => "Can only assert against the cache of a real QueryExecutor instance"
                    );
                }

                var cacheImplementation = qe.CurrentCache;
                var passed = cacheImplementation is MemoryCache;
                return new MatcherResult(
                    passed,
                    () => $"Expected {passed.AsNot()}to find MemoryCache"
                );
            });
        }
    }
}