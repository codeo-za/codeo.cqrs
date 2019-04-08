using System.Collections.Generic;

namespace Codeo.CQRS.Tests.Queries
{
    public class FindCarlSaganAlike : Query<PersonLike>
    {
        public override void Execute()
        {
            Result = SelectFirst<PersonLike>("select * from people where name = 'Carl Sagan';");
        }
    }
    
    public class FindCarlSaganAlikes : Query<IEnumerable<PersonLike>>
    {
        public override void Execute()
        {
            Result = SelectMany<PersonLike>("select * from people where name = 'Carl Sagan';");
        }
    }
}