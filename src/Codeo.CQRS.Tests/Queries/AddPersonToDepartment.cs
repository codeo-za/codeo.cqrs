namespace Codeo.CQRS.Tests.Queries
{
    public class AddPersonToDepartment : InsertCommand<int>
    {
        public int PersonId { get; }
        public int DepartmentId { get; }

        public AddPersonToDepartment(
            int personId,
            int departmentId
        ) : base(@"
            insert into departments_people(person_id, department_id) 
            values (@personId, @departmentId);
            select LAST_INSERT_ID() as id;",
            new
            {
                personId,
                departmentId
            })
        {
            PersonId = personId;
            DepartmentId = departmentId;
        }

        public override void Validate()
        {
        }
    }
}