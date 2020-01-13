namespace Codeo.CQRS.Tests.Queries
{
    public class DeletePerson : DeleteCommand
    {
        public DeletePerson(int id)
            : base(
                "delete from people where id = @id;",
                new { id }
            )
        {
        }
    }
}