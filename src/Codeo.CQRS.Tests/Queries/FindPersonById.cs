using Codeo.CQRS.Tests.Models;

namespace Codeo.CQRS.Tests.Queries
{
    [Cache(60, nameof(Id))]
    public class FindPersonById : Query<Person>
    {
        public int Id { get; }
        public bool ShouldInvalidateCache { get; set; }

        public FindPersonById(int id)
        {
            Id = id;
        }

        public override void Execute()
        {
            if (ShouldInvalidateCache)
            {
                InvalidateCache();
            }

            Result = SelectFirst<Person>(
                "select * from people where id = @id;", new { Id }
            );
        }
    }
    
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