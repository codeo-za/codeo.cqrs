using Codeo.CQRS.Tests.Models;

namespace Codeo.CQRS.Tests.Queries
{
    [Cache(1, nameof(Id))]
    public class FindPersonByIdShortLived : Query<Person>
    {
        public int Id { get; }
        public bool ShouldInvalidateCache { get; set; }

        public FindPersonByIdShortLived(int id)
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

        public override void Validate()
        {
        }
    }
}