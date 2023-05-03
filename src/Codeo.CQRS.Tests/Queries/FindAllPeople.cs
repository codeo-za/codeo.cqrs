using System.Collections.Generic;
using Codeo.CQRS.Tests.Models;

namespace Codeo.CQRS.Tests.Queries
{
    public class FindAllPeople : SelectQuery<IEnumerable<Person>>
    {
        public FindAllPeople() : base("select * from people;")
        {
        }

        public override void Validate()
        {
        }
    }
}