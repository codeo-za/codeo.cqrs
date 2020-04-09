namespace Codeo.CQRS.Tests.Queries
{
    public class CreateDepartment : InsertCommand<int>
    {
        public string Name { get; }

        public CreateDepartment(string name)
            : base(
                @"insert into departments (name) values (@name);
                select LAST_INSERT_ID() as id",
                new
                {
                    name
                })
        {
            Name = name;
        }
    }
}