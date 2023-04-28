namespace Codeo.CQRS.Tests.Commands
{
    // cache should be ignored
    // -> need to test with only Id in the key so we can change the name
    [Cache(60, nameof(Id))]
    public class UpdatePersonName : UpdateCommand
    {
        public int Id { get; }

        public UpdatePersonName(int id, string newName)
            : base(
                "update people set name = @newName where id = @id;",
                new { id, newName }
            )
        {
            Id = id;
        }

        public override void Validate()
        {
        }
    }

    // cache should be ignored
    // -> need to test with only Id in the key so we can change the name
    [Cache(60, nameof(Id))]
    public class UpdatePersonNameWithResult : UpdateCommand<string>
    {
        public int Id { get; }

        public UpdatePersonNameWithResult(
            int id,
            string newName
        ) : base(
            @"update people set name = @newName where id = @id;
                select name from people where id = @id;",
            new { id, newName }
        )
        {
            Id = id;
        }

        public override void Validate()
        {
        }
    }
}