namespace Codeo.CQRS.Tests.Queries
{
    public class FindPersonById : Query<Person>
    {
        public int Id { get; }

        public FindPersonById(int id)
        {
            Id = id;
        }

        public override void Execute()
        {
            Result = SelectFirst<Person>(
                         "select * from people where id = @id;", new {Id})
                     ?? throw new PersonNotFound(Id);
        }
    }
}