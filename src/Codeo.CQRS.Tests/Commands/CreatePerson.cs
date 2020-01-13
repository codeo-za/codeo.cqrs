using System;

namespace Codeo.CQRS.Tests.Commands
{
    // cache attributes should be ignored for inserts!
    [Cache(60, nameof(Name), nameof(Enabled))]
    public class CreatePerson : InsertCommand<int>
    {
        public string Name { get; }
        public bool Enabled { get; }

        private const string sql = @"
            insert into people (name, enabled, created)
            values (@name, @enabled, @created);
            select last_insert_id() as id;
            ";

        public CreatePerson(string name) : this(name, true)
        {
        }

        public CreatePerson(string name, bool enabled)
            : base(sql, new
            {
                name,
                enabled,
                created = DateTime.Now
            })
        {
            Name = name;
            Enabled = enabled;
        }
    }

    [Cache(60, nameof(Name), nameof(Enabled))]
    public class CreatePersonNoResult : InsertCommand
    {
        public string Name { get; }
        public bool Enabled { get; }

        private const string sql = @"
            insert into people (name, enabled, created)
            values (@name, @enabled, @created);
            ";

        public CreatePersonNoResult(string name) : this(name, true)
        {
        }

        public CreatePersonNoResult(string name, bool enabled)
            : base(sql, new
            {
                name,
                enabled,
                created = DateTime.Now
            })
        {
            Name = name;
            Enabled = enabled;
        }
    }

    [Cache(60, nameof(Name), nameof(IdToUpdate), nameof(NewName))]
    public class CreatePersonWithSideEffect : InsertCommand
    {
        public string Name { get; }
        public int IdToUpdate { get; }
        public string NewName { get; }

        public CreatePersonWithSideEffect(
            string name,
            int idToUpdate,
            string newName): base(
            @"insert into people (name, enabled, created)
                values (@name, @enabled, @created);
                update people set name = @newName where id = @idToUpdate;",
            new { name, idToUpdate, newName })
        {
            Name = name;
            IdToUpdate = idToUpdate;
            NewName = newName;
        }
    }
}