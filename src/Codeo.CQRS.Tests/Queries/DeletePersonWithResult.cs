using Codeo.CQRS.Tests.Models;

namespace Codeo.CQRS.Tests.Queries
{
    public class DeletePersonWithResult : DeleteCommand<int>
    {
        public DeletePersonWithResult(int id)
            : base(
                "delete from people where id = @id; select max(id) from people;",
                new { id }
            )
        {
        }
    }

    public class DeletePersonNoResultWithSideEffects : DeleteCommand
    {
        public DeletePersonNoResultWithSideEffects(
            int idToDelete,
            int idToUpdate,
            string newName
        )
            : base(
                @"delete from people where id = @idToDelete;
                    update people set name = @newName where id = @idToUpdate;",
                new { idToDelete, idToUpdate, newName }
            )
        {
        }
    }

    public class DeletePersonWithArbResult : DeleteCommand<Person>
    {
        public DeletePersonWithArbResult(int id, int otherId)
            : base(
                @"delete from people where id = @id; 
                select * from people where id = @otherId;",
                new { id, otherId }
            )
        {
        }
    }
}