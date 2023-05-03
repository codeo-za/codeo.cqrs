namespace Codeo.CQRS.Tests.Commands
{
    [Cache(60, nameof(Name), nameof(IdToUpdate), nameof(NewName))]
    public class CreatePersonWithSideEffect : InsertCommand
    {
        public string Name { get; }
        public int IdToUpdate { get; }
        public string NewName { get; }

        public CreatePersonWithSideEffect(
            string name,
            int idToUpdate,
            string newName): base(
            @"insert into people (name, enabled, created)
                values (@name, @enabled, @created);
                update people set name = @newName where id = @idToUpdate;",
            new { name, idToUpdate, newName })
        {
            Name = name;
            IdToUpdate = idToUpdate;
            NewName = newName;
        }

        public override void Validate()
        {
        }
    }
}