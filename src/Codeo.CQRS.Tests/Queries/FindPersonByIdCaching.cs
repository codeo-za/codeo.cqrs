using System;
using Codeo.CQRS.Tests.Models;

namespace Codeo.CQRS.Tests.Queries
{
    [Cache(60)]
    public class FindPersonByIdCaching : CachingQuery<Person>
    {
        public int Id { get; }

        public FindPersonByIdCaching(
            int id,
            bool useCache
        ) : base(useCache)
        {
            Id = id;
        }

        public override void Execute()
        {
            Result = SelectFirst<Person>(
                "select * from people where id = @id;", new { Id }
            );
        }

        public override void Validate()
        {
            if (Id <= 0)
            {
                throw new ArgumentException(
                    "Invalid Id set",
                    nameof(Id)
                );
            }
        }
    }
}