using Codeo.CQRS.Tests.Models;

namespace Codeo.CQRS.Tests.Queries
{
    public class FindPersonByIdUncached : Query<Person>
    {
        public int Id { get; }

        public FindPersonByIdUncached(int id)
        {
            Id = id;
        }

        public override void Execute()
        {
            Result = SelectFirst<Person>(
                "select * from people where id = @id;", new { Id }
            );
        }
    }
}