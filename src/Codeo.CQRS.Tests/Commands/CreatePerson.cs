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

        public override void Validate()
        {
        }
    }
}