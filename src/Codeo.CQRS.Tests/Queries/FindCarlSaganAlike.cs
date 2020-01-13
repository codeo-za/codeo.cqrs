using System.Collections.Generic;

namespace Codeo.CQRS.Tests.Queries
{
    public class FindCarlSaganAlike : SelectQuery<PersonLike>
    {
        public FindCarlSaganAlike() : base("select * from people where name = 'Carl Sagan';")
        {
        }
    }

    public class FindCarlSaganAlikes : SelectQuery<IEnumerable<PersonLike>>
    {
        public FindCarlSaganAlikes() : base("select * from people where name = 'Carl Sagan';")
        {
        }
    }
}