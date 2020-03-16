using Codeo.CQRS.Tests.Models;

namespace Codeo.CQRS.Tests.Queries
{
    [Cache(60, "BadProp")]
    public class FindPersonByIdWithInvalidCacheProp : SelectQuery<Person>
    {
        public FindPersonByIdWithInvalidCacheProp(int id)
            : base(
                "select * from people where id = @id;",
                new { id }
            )
        {
        }
    }
}