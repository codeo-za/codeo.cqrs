using Codeo.CQRS.Tests.Models;

namespace Codeo.CQRS.Tests.Queries
{
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
}