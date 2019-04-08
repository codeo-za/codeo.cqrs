using System.Collections.Generic;

namespace Codeo.CQRS.Tests.Queries
{
    public class FindAllPeople : Query<IEnumerable<Person>>
    {
        public override void Execute()
        {
            Result = SelectMany<Person>("select * from people;");
        }
    }
}