using System;

namespace Codeo.CQRS.Tests.Commands
{
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
}