using Codeo.CQRS.Tests.Models;

namespace Codeo.CQRS.Tests.Queries
{
    public class FindPersonByName : SelectQuery<Person>
    {
        public string Name { get; }

        public FindPersonByName(string name)
            : base("select * from people where name = @name;", new { name } )
        {
            Name = name;
        }
    }
}