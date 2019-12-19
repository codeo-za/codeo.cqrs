using System;

namespace Codeo.CQRS.Tests.Commands
{
    public class CreatePerson : InsertCommand<int>
    {
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
        }
    }
    public class CreatePersonNoResult : InsertCommand
    {
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
        }
    }
}