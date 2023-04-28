using System.Collections.Generic;
using Codeo.CQRS.Tests.Models;

namespace Codeo.CQRS.Tests.Queries
{
    [Cache(60, nameof(Ids))]
    public class FindPeopleByIds : Query<IEnumerable<Person>>
    {
        public IEnumerable<int> Ids { get; }

        public FindPeopleByIds(IEnumerable<int> ids)
        {
            Ids = ids;
        }

        public override void Execute()
        {
            Result = SelectMany<Person>(
                "select * from people where id in @ids;",
                new { Ids }
            );
        }

        public override void Validate()
        {
        }

        public string GenerateCacheKeyForTesting()
        {
            return GenerateCacheKey();
        }
    }
}