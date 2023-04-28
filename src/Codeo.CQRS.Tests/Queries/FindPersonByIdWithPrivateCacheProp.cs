using Codeo.CQRS.Tests.Models;

namespace Codeo.CQRS.Tests.Queries
{
    [Cache(60, nameof(Id))]
    public class FindPersonByIdWithPrivateCacheProp : SelectQuery<Person>
    {
        private int Id { get; }

        public FindPersonByIdWithPrivateCacheProp(int id)
            : base(
                "select * from people where id = @id;",
                new { id }
            )
        {
            Id = id;
        }

        public override void Validate()
        {
        }
    }
}