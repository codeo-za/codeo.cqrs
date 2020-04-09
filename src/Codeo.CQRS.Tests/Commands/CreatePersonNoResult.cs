using System;

namespace Codeo.CQRS.Tests.Commands
{
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
}